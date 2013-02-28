using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using FullTextIndexer.Common.Lists;
using FullTextIndexer.Common.Logging;
using Tester.Example3.SourceData;

namespace Tester.Example3
{
    public class ArticlesDataLoader
    {
        private IArticleRetriever _articleRetriever;
        private int _maxConsecutiveFailCount;
        private ILogger _logger;
        public ArticlesDataLoader(IArticleRetriever articleRetriever, int maxConsecutiveFailCount, ILogger logger)
        {
            if (articleRetriever == null)
                throw new ArgumentNullException("articleRetriever");
            if (maxConsecutiveFailCount <= 0)
                throw new ArgumentOutOfRangeException("maxConsecutiveFailCount", "must be greater than zero");
            if (logger == null)
                throw new ArgumentNullException("logger");

            _articleRetriever = articleRetriever;
            _maxConsecutiveFailCount = maxConsecutiveFailCount;
            _logger = logger;
        }

        public NonNullImmutableList<Article> GetArticles(string searchTerm, int maxResults)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                throw new ArgumentException("Null/empty searchTerm specified");
            if (maxResults <= 0)
                throw new ArgumentOutOfRangeException("maxResults", "must be greater than zero");

            var overallTimer = new Stopwatch();
            overallTimer.Start();
            var articles = new List<Article>();
            var pageIndex = 0;
            var consecutiveFailCount = 0;
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
                    consecutiveFailCount++;
                    if (consecutiveFailCount >= _maxConsecutiveFailCount)
                        break;
                    continue;
                }
                consecutiveFailCount = 0;

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
                if ((articles.Count >= maxResults) || ((_articleRetriever.PageSize * (pageIndex + 1)) >= articleSet.TotalResultCount))
                {
                    // If we've hit the maxResults limit or if there are no more articles available to retrieve, exit
                    break;
                }
                pageIndex++;

                if (articles.Count > 0)
                {
                    var timePerResultInMillisconds = overallTimer.ElapsedMilliseconds / articles.Count;
                    var remainingArticleCount = Math.Min(articleSet.TotalResultCount, maxResults) - timePerResultInMillisconds;
                    _logger.LogIgnoringAnyError(
                        LogLevel.Info,
                        () => "- Estimated completion: " + GetAsFriendlyTime(TimeSpan.FromMilliseconds(timePerResultInMillisconds * remainingArticleCount))
                    );
                }

                requestTimer.Stop();
                if (requestTimer.ElapsedMilliseconds < 125)
                {
                    // Ensure we don't exceed the requests-per-second limit
                    Thread.Sleep(TimeSpan.FromMilliseconds(125 - requestTimer.ElapsedMilliseconds));
                }
            } while (true);
            return new NonNullImmutableList<Article>((articles.Count > maxResults) ? articles.Take(maxResults) : articles);
        }

        private string GetAsFriendlyTime(TimeSpan duration)
        {
            if (duration.Ticks < 0)
                throw new ArgumentOutOfRangeException("duration", "must be zero or greater");

            var now = DateTime.Now;
            var completionDate = now.Add(duration);
            var display = completionDate.ToString("HH:mm:ss");
            if (completionDate.Date != now)
                display = completionDate.ToString("yyyy-MM-dd ") + display;
            return display;
        }
    }
}
