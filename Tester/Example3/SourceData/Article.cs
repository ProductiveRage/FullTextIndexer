using System;

namespace Tester.Example3.SourceData
{
    [Serializable]
    public class Article
    {
        public Article(int key, string title, string byLine, string keywords, string body)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentNullException("Null/empty title specified");
            if (string.IsNullOrWhiteSpace(body))
                throw new ArgumentNullException("Null/empty body specified");

            Key = key;
            Title = title.Trim();
            ByLine = (byLine ?? "").Trim();
            Keywords = (keywords ?? "").Trim();
            Body = body.Trim();
        }
        
        public int Key { get; private set; }

        /// <summary>
        /// This will never be null or empty
        /// </summary>
        public string Title { get; private set; }

        /// <summary>
        /// This will never be null but it may be empty
        /// </summary>
        public string ByLine { get; private set; }

        /// <summary>
        /// This will never be null but it may be empty
        /// </summary>
        public string Keywords { get; private set; }

        /// <summary>
        /// This will never be null or empty
        /// </summary>
        public string Body { get; private set; }
    }
}
