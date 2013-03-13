using System;
using FullTextIndexer.Common.Lists;
using FullTextIndexer.Core.Indexes;
using FullTextIndexer.Querier.QueryAnalysers.ContentAnalysers;
using FullTextIndexer.Querier.QueryAnalysers.StringNavigators;
using FullTextIndexer.Querier.QueryTranslators;

namespace FullTextIndexer.Querier
{
	public class Querier<TSource, TKey> : IQuerier<TSource, TKey>
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
