using System;

namespace FullTextIndexer.Core.IndexGenerators
{
    /// <summary>
    /// This is responsible for retrieving a content section from a source item and assigning a weight to each token that results from breaking the content. It is not
    /// responsible for the breaking of the content.
    /// </summary>
    public class ContentRetriever<TSource, TKey>
    {
        public ContentRetriever(PreBrokenTokenContentRetriever initialContentRetriever, BrokenTokenWeightDeterminer tokenWeightDeterminer)
        {
            InitialContentRetriever = initialContentRetriever ?? throw new ArgumentNullException(nameof(initialContentRetriever));
            TokenWeightDeterminer = tokenWeightDeterminer ?? throw new ArgumentNullException(nameof(tokenWeightDeterminer));
        }

        /// <summary>
        /// This will never be null
        /// </summary>
        public PreBrokenTokenContentRetriever InitialContentRetriever { get; private set; }

        /// <summary>
        /// This will never be null
        /// </summary>
        public BrokenTokenWeightDeterminer TokenWeightDeterminer { get; private set; }

        /// <summary>
		/// This will never be provided a null source value. If the content retriever does not identify any content it is must return a PreBrokenContent with an
		/// empty Content set (rather than returning null).
        /// </summary>
        public delegate PreBrokenContent<TKey> PreBrokenTokenContentRetriever(TSource source);

        /// <summary>
        /// This must always return a value greater than zero, it will never be provided a null or empty token.
        /// </summary>
        public delegate float BrokenTokenWeightDeterminer(string normalisedToken);
    }
}
