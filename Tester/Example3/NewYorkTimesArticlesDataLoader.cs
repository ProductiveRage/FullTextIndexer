using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Common.Lists;
using Common.Logging;
using Tester.Example3.SourceData;

namespace Tester.Example3
{
    public class ArticlesDataLoader
    {
        private IArticleRetriever _articleRetriever;
        private ILogger _logger;
        public ArticlesDataLoader(IArticleRetriever articleRetriever, ILogger logger)
        {
            if (articleRetriever == null)
                throw new ArgumentNullException("articleRetriever");
            if (logger == null)
                throw new ArgumentNullException("logger");

            _articleRetriever = articleRetriever;
            _logger = logger;
        }

        public NonNullImmutableList<Article> GetArticles(string searchTerm, int maxResults)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                throw new ArgumentException("Null/empty searchTerm specified");
            if (maxResults <= 0)
                throw new ArgumentOutOfRangeException("maxResults", "must be greater than zero");

            var articles = new List<Article>();
            var pageIndex = 0;
            do
            {
                var requestTimer = new Stopwatch();
                requestTimer.Start();

                ArticleSet articleSet;
                try
                {
                    articleSet = _articleRetriever.GetArticles(searchTerm, pageIndex);
                    if (!articleSet.Articles.Any())
                        break;
                }
                catch
                {
                    _logger.LogIgnoringAnyError(LogLevel.Warning, () => "A page request failed, continuing to next page..");
                    continue;
                }

                _logger.LogIgnoringAnyError(
                    LogLevel.Info,
                    () => String.Format(
                        "[{0}/{1}]: : Got {2} article(s)..",
                        pageIndex + 1,
                        Math.Min(articleSet.TotalResultCount, maxResults) / _articleRetriever.PageSize,
                        articleSet.Articles.Count
                    )
                );
                articles.AddRange(articleSet.Articles);
                if (articles.Count >= maxResults)
                    break;
                pageIndex++;

                requestTimer.Stop();
                if (requestTimer.ElapsedMilliseconds < 125)
                {
                    // Ensure we don't exceed the requests-per-second limit
                    Thread.Sleep(TimeSpan.FromMilliseconds(125 - requestTimer.ElapsedMilliseconds));
                }
            } while (true);
            return new NonNullImmutableList<Article>((articles.Count > maxResults) ? articles.Take(maxResults) : articles);
        }
    }
}
