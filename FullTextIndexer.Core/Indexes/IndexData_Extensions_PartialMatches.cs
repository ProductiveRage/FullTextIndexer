using System;
using System.Collections.Generic;
using System.Linq;
using FullTextIndexer.Common.Lists;
using FullTextIndexer.Core.TokenBreaking;

namespace FullTextIndexer.Core.Indexes
{
	public static class IndexData_Extensions_PartialMatches
    {
		/// <summary>
		/// This will break a given source string and return results based upon the combination of partial matches (so results that only match part of the source string may be included
		/// in the returned data). The token breaker and the match combiner must be specified by the caller - if the match combiner returns zero then the result will not be included in
		/// the final data. To require that all tokens in the source content be present for any returned results, the following matchCombiner could be specified:
		///  (tokenMatches, allTokens) => (tokenMatches.Count &lt; allTokens.Count) ? 0 : tokenMatches.Sum(m => m.Weight)
		/// </summary>
		public static NonNullImmutableList<WeightedEntry<TKey>> GetPartialMatches<TKey>(
			this IIndexData<TKey> index,
			string source,
			ITokenBreaker tokenBreaker,
			WeightCombiner weightCombiner)
		{
			if (index == null)
				throw new ArgumentNullException("index");
			if (source == null)
				throw new ArgumentNullException("source");
			if (tokenBreaker == null)
				throw new ArgumentNullException("tokenBreaker");
			if (weightCombiner == null)
				throw new ArgumentNullException("weightCombiner");

			// Break down the "source" search term and find matches for each token
			// - Each match maintains the weight multiplier applied to the string segment from the token breaker
			// - The Source Locations are annotated with additional data; the source segment string and what token index that is (so if the "source" value is broken into three, then
			//   each Source Location will have a SearchTerm property whose TokenIndex will be between 0 and 2, inclusive). This allows for a weightCombiner to be specified that
			//   ensures that every token that was extract from the source value can be matched against a given result, if so desired.
			var matches = new List<Tuple<WeightedEntry<TKey>, SearchTermDetails>>();
			var weightAdjustedTokens = tokenBreaker.Break(source);
			for (var tokenIndex = 0; tokenIndex < weightAdjustedTokens.Count; tokenIndex++)
			{
				var weightAdjustedToken = weightAdjustedTokens[tokenIndex];
				matches.AddRange(
					index
						.GetMatches(weightAdjustedToken.Token)
						.Select(match => Tuple.Create(match, new SearchTermDetails(tokenIndex, weightAdjustedToken.Token)))
				);
			}

			// Combine per-search-term results, grouping by result key and calculating the match weight for each token using the specified weightCombiner (this may also be
			// used to filter out results; if a match weight of zero is returned then the match will be ignored - this may used to filter out results that only match two
			// out of three of the search terms, for example)
			var finalResults = NonNullImmutableList<WeightedEntry<TKey>>.Empty;
			var searchTerms = new NonNullOrEmptyStringList(weightAdjustedTokens.Select(w => w.Token));
			foreach (var matchesGroupedByKey in matches.GroupBy(m => m.Item1.Key, index.KeyComparer).Cast<IEnumerable<Tuple<WeightedEntry<TKey>, SearchTermDetails>>>())
			{
				var combinedWeight = weightCombiner(
					matchesGroupedByKey
						.Select(m => new MatchWeightWithSourceFieldLocations(
							m.Item1.Weight,
							m.Item2,
							m.Item1.SourceLocationsIfRecorded
						)).ToNonNullImmutableList(),
					searchTerms
				);
				if (combinedWeight < 0)
					throw new ArgumentException("weightCombiner returned a negative value - invalid");
				else if (combinedWeight > 0)
				{
					finalResults = finalResults.Add(
						new WeightedEntry<TKey>(
							matchesGroupedByKey.First().Item1.Key,
							combinedWeight,
							matchesGroupedByKey.Any(m => m.Item1.SourceLocationsIfRecorded == null)
								? null
								: matchesGroupedByKey.SelectMany(m => m.Item1.SourceLocationsIfRecorded).ToNonNullImmutableList()
						)
					);
				}
			}
			return finalResults;
		}

		/// <summary>
		/// This GetPartialMatches signature will call GetPartialMatches specifying the DefaultWeightCombiner for the weightCombiner argument
		/// </summary>
		public static NonNullImmutableList<WeightedEntry<TKey>> GetPartialMatches<TKey>(this IIndexData<TKey> index, string source, ITokenBreaker tokenBreaker)
		{
			return GetPartialMatches(index, source, tokenBreaker, DefaultWeightCombiner);
		}

		/// <summary>
		/// This GetPartialMatches signature will call GetPartialMatches specifying the DefaultWeightCombiner for the weightCombiner argument and the DefaultTokenBreaker
		/// for the tokenBreaker
		/// </summary>
		public static NonNullImmutableList<WeightedEntry<TKey>> GetPartialMatches<TKey>(this IIndexData<TKey> index, string source)
		{
			return GetPartialMatches(index, source, DefaultTokenBreaker);
		}

		/// <summary>
		/// Given the complete set of match data for a particular result, this must determine the combined match weight. It must return zero or a positive value, if it
		/// returns zero then the result will be excluded from the set returned from the GetPartialMatches method. It may be desirable to exclude results that don't
		/// match all of the search terms at least once (this can be determined by considering the SearchTermDetails of the SourceLocations data and checking that
		/// TokenIndex values 0 to searchTerms.Count-1 all are present at least once).
		/// </summary>
		public delegate float WeightCombiner(
			NonNullImmutableList<MatchWeightWithSourceFieldLocations> matchSourceLocations,
			NonNullOrEmptyStringList searchTerms
		);

		/// <summary>
		/// This will add up the match weights so long as every search term is matched at least once (if not then a combined match weight of zero will be returned,
		/// indicating that the match should not be included in the final results)
		/// </summary>
		public static WeightCombiner DefaultWeightCombiner
		{
			get
			{
				return (matchSourceLocations, searchTerms) =>
				{
					if (matchSourceLocations == null)
						throw new ArgumentNullException("matchSourceLocations");
					if (searchTerms == null)
						throw new ArgumentNullException("searchTerms");

					// Ensure that all of the search terms are present for the current result (take all of the SearchTerm.TokenIndex values for the source
					// locations and then ensure that all of 0.. (searchTerms.Count-1) are present in that set
					var searchTermTokenIndexes = matchSourceLocations.Select(s => s.SearchTerm.TokenIndex).ToArray();
					for (var tokenIndex = 0; tokenIndex < searchTerms.Count; tokenIndex++)
					{
						if (!searchTermTokenIndexes.Contains(tokenIndex))
							return 0;
					}
					return matchSourceLocations.Sum(m => m.Weight);
				};
			}
		}

		/// <summary>
		/// Note: This is consistent with the default AutomatedIndexGeneratorFactoryBuilder TokenBreaker - a WhiteSpaceExtendingTokenBreaker with various
		/// punctuation marks identified to be considered as whitespace
		/// </summary>
		public static ITokenBreaker DefaultTokenBreaker
		{
			get
			{
				return new WhiteSpaceExtendingTokenBreaker(
					new ImmutableList<char>(
						'<', '>', '[', ']', '(', ')', '{', '}',
						'.', ',', ':', ';', '"', '?', '!',
						'/', '\\',
						'@', '+', '|', '='
					),
					new WhiteSpaceTokenBreaker()
				);
			}
		}

		/// <summary>
		/// This represents some of the match data for a particular token (extracted from the "source" argument passed to the GetPartialMatches method) for a particular result)
		/// </summary>
		public sealed class MatchWeightWithSourceFieldLocations
		{
			public MatchWeightWithSourceFieldLocations(float weight, SearchTermDetails searchTerm, NonNullImmutableList<SourceFieldLocation> sourceLocationsIfRecorded)
			{
				if (weight <= 0)
					throw new ArgumentOutOfRangeException(nameof(weight), "must be greater than zero");
				if ((sourceLocationsIfRecorded != null) && !sourceLocationsIfRecorded.Any())
					throw new ArgumentException("must not be empty if it is non-null", nameof(sourceLocationsIfRecorded));

				Weight = weight;
				SearchTerm = searchTerm ?? throw new ArgumentNullException(nameof(searchTerm));
				SourceLocationsIfRecorded = sourceLocationsIfRecorded;
			}

			/// <summary>
			/// This will always be greater than zero
			/// </summary>
			public float Weight { get; }

			/// <summary>
			/// This will never be null
			/// </summary>
			public SearchTermDetails SearchTerm { get; private set; }

			/// <summary>
			/// This will be null if the source location data is not recorded by the index generator but it will never be an empty list if it is not null
			/// </summary>
			public NonNullImmutableList<SourceFieldLocation> SourceLocationsIfRecorded { get; }
		}

		public sealed class SearchTermDetails
		{
			public SearchTermDetails(int tokenIndex, string searchTerm)
			{
				if (tokenIndex < 0)
					throw new ArgumentOutOfRangeException("tokenIndex", "must be zero or greater");
				if (string.IsNullOrWhiteSpace(searchTerm))
					throw new ArgumentException("Null/blank searchTerm specified");

				TokenIndex = tokenIndex;
				SearchTerm = searchTerm;
			}

			/// <summary>
			/// Where this search term was the result of breaking down a longer search term, this is the index of the token from the source term. It will will always be
			/// zero or greater.
			/// </summary>
			public int TokenIndex { get; private set; }

			/// <summary>
			/// This will never be null or blank
			/// </summary>
			public string SearchTerm { get; private set; }
		}
	}
}