using System;
using FullTextIndexer.Common.Lists;

namespace FullTextIndexer.Core
{
	public static class Constants
    {
        public static NonNullOrEmptyStringList GetStopWords(string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
                throw new ArgumentException("Null/empty languageCode");

            // Currently only support English stopwords
            if (languageCode.Equals("en", StringComparison.OrdinalIgnoreCase))
                return EnglishStopWords;
            return NonNullOrEmptyStringList.Empty;
        }
            
        // "Borrowed" these from Lucene.Net
        private static readonly NonNullOrEmptyStringList EnglishStopWords = new NonNullOrEmptyStringList(
            new[] { "a", "an", "and", "are", "as", "at", "be", "but", "by", "for", "if", "in", "into", "is", "it", "no", "not", "of", "on", "or", "such", "that", "the", "their", "then", "there", "these", "they", "this", "to", "was", "will", "with" }
        );
    }
}
