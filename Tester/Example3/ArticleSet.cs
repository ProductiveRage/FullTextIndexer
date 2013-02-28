using System;
using System.Collections.Generic;
using System.Linq;
using FullTextIndexer.Common.Lists;
using Tester.Example3.SourceData;

namespace Tester.Example3
{
    public class ArticleSet
    {
        public ArticleSet(int totalResultCount, int pageIndex, IEnumerable<Article> articles)
        {
            if (totalResultCount < 0)
                throw new ArgumentOutOfRangeException("totalResultCount", "must be zero or greater");
            if (pageIndex < 0)
                throw new ArgumentOutOfRangeException("pageIndex", "must be zero or greater");
            if (articles == null)
                throw new ArgumentNullException("articles");

            var articlesTidied = new List<Article>();
            foreach (var article in articles)
            {
                if (article == null)
                    throw new ArgumentException("Null entry encountered in articles");
                articlesTidied.Add(article);
            }
            if ((totalResultCount == 0) && articlesTidied.Any())
                throw new ArgumentException("If totalResultCount is zero then the articles set must be empty");

            TotalResultCount = totalResultCount;
            PageIndex = pageIndex;
            Articles = articlesTidied.ToNonNullImmutableList();
        }

        /// <summary>
        /// This will always be zero or greater
        /// </summary>
        public int TotalResultCount { get; private set; }

        /// <summary>
        /// This will always be zero or greater
        /// </summary>
        public int PageIndex { get; private set; }

        /// <summary>
        /// This will never be null. If TotalResultCount is zero then this will always be empty. If TotalResultCount is non-zero this may still be empty if a Page
        /// Index beyond the available results was specified.
        /// </summary>
        public NonNullImmutableList<Article> Articles { get; private set; }
    }
}
