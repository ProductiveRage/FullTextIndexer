using System;
using FullTextIndexer.Common.Lists;
using FullTextIndexer.Core.Indexes;
using FullTextIndexer.Querier.QueryAnalysers.ContentAnalysers;
using FullTextIndexer.Querier.QueryAnalysers.StringNavigators;
using FullTextIndexer.Querier.QueryTranslators;

namespace FullTextIndexer.Querier
{
	/// <summary>
	/// This is used to query for multiple terms in a structured manner - for example,
	/// 
	///   +(apples pears bananas) +fruit +nut -orange
	///   
	/// will look for documents that contain at least one of "apples", "pears" and "bananas" AND contain "fruit AND contain "nut" but do NOT contain "orange".
	/// If terms are quoted, then precise matching is required. The search term:
	/// 
	///   apples
	///   
	/// may be matched to "apples" OR to "apple" but the search term
	/// 
	///   "apples"
	///   
	/// may ONLY be matched precisely to "apples", the word "apple" will not be considered a sufficiently good match.
	/// 
	/// In order to do this, two indexes must be generated - the standardMatchIndexData should be constructed by extracting single word terms and using a
	/// relatively lenient string normaliser (such as the EnglishPluralityStringNormaliser) while the preciseMatchIndexData should use a more stringent string
	/// normaliser (such as the DefaultStringNormaliser) and should not only break down documents into single words but should also re-construct some runs of
	/// words into multi-word tokens (for which the ConsecutiveTokenCombiningTokenBreaker may be used) for cases where quoting multiple words must be supported
	/// - eg.
	/// 
	///   "apples and pears" +fruit +nut -orange
	///   
	/// will require that documents contain the consecutive tokens "apples and pears" AND "fruit" (or an acceptably similar word, such as "fruits" - depending
	/// upon the string normaliser that the standardMatchIndexData uses) AND "nut" (or similar) but NOT "orange" (or similar).
	/// </summary>
	public class Querier<TKey> : IQuerier<TKey>
	{
		private readonly IQueryTranslator<TKey> _queryTranslator;
		public Querier(IIndexData<TKey> standardMatchIndexData, IIndexData<TKey> preciseMatchIndexData, QueryTranslator<TKey>.MatchCombiner matchCombiner)
		{
			if (standardMatchIndexData == null)
				throw new ArgumentNullException("standardMatchIndexData");
			if (preciseMatchIndexData == null)
				throw new ArgumentNullException("preciseMatchIndexData");
			if (matchCombiner == null)
				throw new ArgumentNullException("matchCombiner");

			_queryTranslator = new QueryTranslator<TKey>(
				standardMatchIndexData,
				preciseMatchIndexData,
				matchCombiner
			);
		}

		/// <summary>
		/// This will never return null. It will throw an exception for a null or blank searchTerm.
		/// </summary>
		public NonNullImmutableList<WeightedEntry<TKey>> GetMatches(string searchTerm)
		{
			if (string.IsNullOrWhiteSpace(searchTerm))
				throw new ArgumentException("Null/blank searchTerm specified");

			var processedQuerySegment = (new BreakPointCharacterAnalyser()).Process(
				new StringNavigator(searchTerm)
			);
			return _queryTranslator.GetMatches(
				processedQuerySegment.QuerySegment
			);
		}
	}
}
