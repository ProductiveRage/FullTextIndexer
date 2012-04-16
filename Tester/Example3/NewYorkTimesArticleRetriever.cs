using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Web;
using System.Web.Script.Serialization;
using Tester.Example3.SourceData;

namespace Tester.Example3
{
    public class NewYorkTimesArticleRetriever : IArticleRetriever
    {
        private string _apiKey;
        public NewYorkTimesArticleRetriever(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("Null/empty apiKey specified");

            _apiKey = apiKey.Trim();
        }

        /// <summary>
        /// This will never return null. It will throw an exception if the request failed. If no results were available then an Articleset with a zero
        /// TotalResultCount will be returned.
        /// </summary>
        public ArticleSet GetArticles(string searchTerm, int pageIndex)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                throw new ArgumentException("Null/empty searchTerm specified");
            if (pageIndex < 0)
                throw new ArgumentOutOfRangeException("pageIndex", "must be zero or greater");

            var request = WebRequest.Create(string.Format(
                "http://api.nytimes.com/svc/search/v1/article?query={0}&api-key={1}&fields=column_facet,body,title,byline&offset={2}",
                HttpUtility.UrlEncode(searchTerm.Trim()),
                HttpUtility.UrlEncode(_apiKey.Trim()),
                pageIndex
            ));

            string jsonData;
            using (var stream = request.GetResponse().GetResponseStream())
            {
                using (var reader = new StreamReader(stream))
                {
                    jsonData = reader.ReadToEnd();
                }
            }
            var results = new JavaScriptSerializer().Deserialize<Resultset>(jsonData);
            var translatedArticles = new List<Article>();
            for (var index = 0; index < results.Results.Count; index++)
            {
                var article = results.Results[index];
                if (string.IsNullOrWhiteSpace(article.Title) || string.IsNullOrWhiteSpace(article.Body))
                    continue;
                translatedArticles.Add(new Article(
                    (pageIndex * PageSize) + index,
                    article.Title,
                    article.ByLine,
                    article.Column_Facet,
                    article.Body
                ));
            }
            return new ArticleSet(results.Total, pageIndex, translatedArticles);
        }

        /// <summary>
        /// This will always be one or greater
        /// </summary>
        public int PageSize { get { return 10; } }

        private class Resultset
        {
            public Resultset()
            {
                Results = new List<Result>();
            }
            public int Offset { get; set; }
            public int Total { get; set; }
            public string[] Tokens { get; set; }
            public List<Result> Results { get; set; }

            public class Result
            {
                public string Title { get; set; }
                public string ByLine { get; set; }
                public string Column_Facet { get; set; }
                public string Body { get; set; }
            }
        }
    }
}
