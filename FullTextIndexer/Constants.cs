using Common.Lists;

namespace FullTextIndexer
{
    public static class Constants
    {
        // "Borrowed" these from Lucene.Net
        public static NonNullOrEmptyStringList StopWords = new NonNullOrEmptyStringList(
            new[] { "a", "an", "and", "are", "as", "at", "be", "but", "by", "for", "if", "in", "into", "is", "it", "no", "not", "of", "on", "or", "such", "that", "the", "their", "then", "there", "these", "they", "this", "to", "was", "will", "with" }
        );
    }
}
