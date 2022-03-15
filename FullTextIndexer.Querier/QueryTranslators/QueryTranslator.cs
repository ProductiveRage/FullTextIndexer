using System;
using System.Collections.Generic;
using System.Linq;
using FullTextIndexer.Common.Lists;
using FullTextIndexer.Core.Indexes;
using FullTextIndexer.Core.IndexGenerators;
using FullTextIndexer.Core.TokenBreaking;
using FullTextIndexer.Querier.Misc;
using FullTextIndexer.Querier.QuerySegments;

namespace FullTextIndexer.Querier.QueryTranslators
{
	/// <summary>
	/// This will retrieve results from index data that match the specified IQuerySegment implementation requirements. Two index data sets are required; the
	/// preciseMatchIndexData is used for retrieving results from PreciseMatchQuerySegment data while other data is retrieved from the standardMatchIndexData.
	/// These two indexes should have the same source data (though likely different processing methods to build the index data) and both share the same key
	/// comparer. Data retrievals from the preciseMatchIndexData will use the GetConsecutiveMatches method since these are expected to describe quote values
	/// (and so may have multiple words within a single query segment). The weight combiners and token breaker for the GetConsecutiveMatchese may be specified
	/// or left as null to use the defaults.
	/// 
	/// The matchCombiner is used to ensure that any set of query segments returns only one WeightedEntry for any key. If a key appears multiple
	/// times in different nested query segments then the combination of results for that key may occur multiple times - this allows, depending upon the
	/// MatchCombiner implementation, to give lower weight to results the deeper nested that the originating query segments are (query segments are said
	/// to be nested if they are among the query segments found in a CombiningQuerySegment). There is a method signature where this matchCombiner need not
	/// be specified, in which case a default will be applied that combines weighted entries by summing their weights.
	///
	/// Note: This requires the preciseMatchIndexData to have been built with source location data recorded - if its SourceLocationsAvailable property returns
	/// false then an ArgumentException will be thrown.
	/// </summary>
	public class QueryTranslator<TKey> : IQueryTranslator<TKey>
	{
		private readonly CachingResultMatcher _standardMatcher;
		private readonly CachingResultMatcher _preciseMatcher;
		private readonly IEqualityComparer<TKey> _keyComparer;
		private readonly MatchCombiner _matchCombiner;
		public QueryTranslator(
			IIndexData<TKey> standardMatchIndexData,
			IIndexData<TKey> preciseMatchIndexData,
			ITokenBreaker optionalQuotedValueConsecutiveTermTokenBreaker,
			IndexGenerator.WeightedEntryCombiner optionalQuotedValueConsecutiveWeightCombinerForConsecutiveRuns,
			IndexGenerator.WeightedEntryCombiner optionalQuotedValueConsecutiveWeightCombinerForFinalMatches,
			MatchCombiner matchCombiner)
		{
			if (standardMatchIndexData == null)
				throw new ArgumentNullException(nameof(standardMatchIndexData));
			if (preciseMatchIndexData == null)
				throw new ArgumentNullException(nameof(preciseMatchIndexData));
            if (!preciseMatchIndexData.SourceLocationsAvailable)
				throw new ArgumentException($"The {nameof(preciseMatchIndexData)} must include source location data in order to use the Query Translator");

			// Can't actually determine for sure that the KeyComparer of the standardMatchIndexData is equivalent to that of the preciseMatchIndexData
			// (can't do an instance comparison since they may be different instances of the same implementation, they could even feasibly be different
			// classes with identical functionality) so we'll have to assume that the caller is behaving themselves. We'll take the KeyComparer of the
			// standardMatchIndexData for use when combining keys, excluding keys or otherwise processing the query segment requirements.
			_standardMatcher = new CachingResultMatcher(standardMatchIndexData.GetMatches);
			_preciseMatcher = new CachingResultMatcher(
				source => preciseMatchIndexData.GetConsecutiveMatches(
					source,
					optionalQuotedValueConsecutiveTermTokenBreaker ?? IndexData_Extensions_ConsecutiveMatches.DefaultTokenBreaker,
					optionalQuotedValueConsecutiveWeightCombinerForConsecutiveRuns ?? IndexData_Extensions_ConsecutiveMatches.DefaultConsecutiveRunsWeightCombiner,
					optionalQuotedValueConsecutiveWeightCombinerForFinalMatches ?? IndexData_Extensions_ConsecutiveMatches.DefaultFinalMatchWeightCombiner
				)
			);
			_keyComparer = standardMatchIndexData.KeyComparer;
			_matchCombiner = matchCombiner ?? throw new ArgumentNullException(nameof(matchCombiner));
		}
		public QueryTranslator(
			IIndexData<TKey> standardMatchIndexData,
			IIndexData<TKey> preciseMatchIndexData,
			ITokenBreaker optionalQuotedValueConsecutiveTermTokenBreaker,
			MatchCombiner matchCombiner)
			: this(standardMatchIndexData, preciseMatchIndexData, optionalQuotedValueConsecutiveTermTokenBreaker, null, null, matchCombiner) { }
		public QueryTranslator(
			IIndexData<TKey> standardMatchIndexData,
			IIndexData<TKey> preciseMatchIndexData,
			MatchCombiner matchCombiner)
			: this(standardMatchIndexData, preciseMatchIndexData, null, matchCombiner) { }
		public QueryTranslator(
			IIndexData<TKey> standardMatchIndexData,
			IIndexData<TKey> preciseMatchIndexData)
			: this(standardMatchIndexData, preciseMatchIndexData, DefaultMatchCombiner) { }

		public static MatchCombiner DefaultMatchCombiner
		{
			get
			{
				return (matchWeights, sourceQuerySegments) => matchWeights.Sum();
			}
		}

		/// <summary>
		/// This will never be called with null or empty matchWeights or sourceQuerySegments lists. It must always return a value greater than zero.
		/// The sourceQuerySegments set will contain all query segments in the current group - if this is a nested (bracketed) set of query terms
		/// then the terms in that bracketed set will be included, not any parent segments. This data is included so that a lower weight may be
		/// applied to nested query terms by dividing the combined weights by the number of segments in the set, for example.
		/// </summary>
		public delegate float MatchCombiner(ImmutableList<float> matchWeights, NonNullImmutableList<IQuerySegment> sourceQuerySegments);

		/// <summary>
		/// This will never return null but may return an empty set if no matches could be made. An exception will be raised for a null querySegment
		/// reference of if the request could otherwise not be satisfied (eg. unsupported IQuerySegment implementation)
		/// </summary>
		public NonNullImmutableList<WeightedEntry<TKey>> GetMatches(IQuerySegment querySegment)
		{
			if (querySegment == null)
				throw new ArgumentNullException(nameof(querySegment));

            if (querySegment is CombiningQuerySegment combiningQuerySegment)
                return Reduce(combiningQuerySegment.Segments);

            return Reduce(NonNullImmutableList.Create(querySegment));
		}

		private NonNullImmutableList<WeightedEntry<TKey>> Reduce(NonNullImmutableList<IQuerySegment> querySegments)
		{
			if (querySegments == null)
				throw new ArgumentNullException(nameof(querySegments));

			HashSet<TKey> compulsoryKeys = null;
			var exclusionKeys = new HashSet<TKey>(_keyComparer);
			var allInclusiveWeighedMatches = new List<WeightedEntry<TKey>>();
			foreach (var querySegment in querySegments)
			{
                if (querySegment is CombiningQuerySegment combiningQuerySegment)
                {
                    allInclusiveWeighedMatches.AddRange(
                        Reduce(combiningQuerySegment.Segments)
                    );
                    continue;
                }

                if (querySegment is CompulsoryQuerySegment compulsoryQuerySegment)
                {
                    var compulsoryQuerySegmentKeys = GetMatches(compulsoryQuerySegment.Segment);
                    var keysForCurrentSegment = compulsoryQuerySegmentKeys.Select(e => e.Key);
                    if (compulsoryKeys == null)
                        compulsoryKeys = new HashSet<TKey>(keysForCurrentSegment, _keyComparer);
                    else
                        compulsoryKeys.IntersectWith(keysForCurrentSegment);
                    allInclusiveWeighedMatches.AddRange(compulsoryQuerySegmentKeys);
                    continue;
                }

                if (querySegment is ExcludingQuerySegment excludingQuerySegment)
                {
                    exclusionKeys.AddRange(
                        GetMatches(excludingQuerySegment.Segment).Select(e => e.Key)
                    );
                    continue;
                }

                if (querySegment is NoMatchContentQuerySegment)
					continue;

                if (querySegment is PreciseMatchQuerySegment preciseMatchQuerySegment)
                {
                    // Since the quoted value could contain multiple terms we'll need to call 
                    allInclusiveWeighedMatches.AddRange(
                        _preciseMatcher.GetMatches(preciseMatchQuerySegment.Value)
                    );
                    continue;
                }

                if (querySegment is StandardMatchQuerySegment standardMatchQuerySegment)
                {
                    allInclusiveWeighedMatches.AddRange(
                        _standardMatcher.GetMatches(standardMatchQuerySegment.Value)
                    );
                    continue;
                }

                throw new NotSupportedException("Unsupported IQuerySegment type: " + querySegment.GetType());
			}

			// Filter the matches to respect the exclusionKeys (remove them entirely) and compulsoryKeys (remove any not specified) data
			var combinedKeys = allInclusiveWeighedMatches.Where(e => !exclusionKeys.Contains(e.Key));
			if (compulsoryKeys != null)
				combinedKeys = combinedKeys.Where(e => compulsoryKeys.Contains(e.Key));
			
			// Group the data so that no key appears more than once by combining the match weights
			return combinedKeys
				.GroupBy(e => e.Key, _keyComparer)
				.Select(group => new WeightedEntry<TKey>(
					group.Key,
					_matchCombiner(group.Select(e => e.Weight).ToImmutableList(), querySegments),
					group.Any(e => e.SourceLocationsIfRecorded == null) ? null : group.SelectMany(e => e.SourceLocationsIfRecorded).ToNonNullImmutableList()
				))
				.ToNonNullImmutableList();
		}

		private class CachingResultMatcher
		{
			private readonly Func<string, NonNullImmutableList<WeightedEntry<TKey>>> _matchRetriever;
			private readonly Dictionary<string, NonNullImmutableList<WeightedEntry<TKey>>> _cache;
			public CachingResultMatcher(Func<string, NonNullImmutableList<WeightedEntry<TKey>>> matchRetriever)
			{
                _matchRetriever = matchRetriever ?? throw new ArgumentNullException(nameof(matchRetriever));
				_cache = new Dictionary<string, NonNullImmutableList<WeightedEntry<TKey>>>();
			}

			public NonNullImmutableList<WeightedEntry<TKey>> GetMatches(string source)
			{
				if (source == null)
					throw new ArgumentNullException(nameof(source));

				lock (_cache)
				{
					if (_cache.TryGetValue(source, out var cachedData))
						return cachedData;
				}

				var data = _matchRetriever(source);
				lock (_cache)
				{
					if (!_cache.ContainsKey(source))
						_cache.Add(source, data);
				}
				return data;
			}
		}
	}
}
