namespace Tester.Example3
{
    public interface IArticleRetriever
    {
        /// <summary>
        /// This will never return null. It will throw an exception if the request failed. If no results were available then an Articleset with a zero
        /// TotalResultCount will be returned.
        /// </summary>
        ArticleSet GetArticles(string searchTerm, int pageIndex);

        /// <summary>
        /// This will always be one or greater
        /// </summary>
        int PageSize { get; }
    }
}
