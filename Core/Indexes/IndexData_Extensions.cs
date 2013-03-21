using System;
using System.Collections.Generic;
using System.Linq;
using FullTextIndexer.Common.Lists;
using FullTextIndexer.Core.TokenBreaking;

namespace FullTextIndexer.Core.Indexes
{
    public static class IndexData_Extensions
    {
        /// <summary>
        /// This will break a given source string and return results based upon the combination of partial matches (so results that only match part of the source string may be included
        /// in the returned data). The token breaker and the match combiner must be specified by the caller - if the match combiner returns zero then the result will not be included in
        /// the final data. To require that all tokens in the source content be present for any returned results, the following matchCombiner could be specified:
        ///  (tokenMatches, allTokens) => (tokenMatches.Count &lt; allTokens.Count) ? 0 : tokenMatches.SelectMany(m => m.Weights).Sum()
        /// </summary>
		public static NonNullImmutableList<WeightedEntryWithTerm<TKey>> GetPartialMatches<TKey>(
            this IIndexData<TKey> index,
            string source,
            ITokenBreaker tokenBreaker,
            MatchCombiner matchCombiner)
        {
            if (index == null)
                throw new ArgumentNullException("index");
            if (source == null)
                throw new ArgumentNullException("source");
            if (tokenBreaker == null)
                throw new ArgumentNullException("tokenBreaker");
            if (matchCombiner == null)
                throw new ArgumentNullException("matchCombiner");

            // Break down the source string and look for matches in the data, group the results by data key
			var allMatchesByKey = new Dictionary<TKey, Dictionary<string, List<WeightedEntryWithTerm<TKey>>>>(
                index.KeyComparer
            );
			var weightAdjustedTokens = tokenBreaker.Break(source);
            foreach (var weightAdjustedToken in weightAdjustedTokens)
            {
                foreach (var match in index.GetMatches(weightAdjustedToken.Token))
                {
                    // Initialise / retrieve all token data for particular match by key
					Dictionary<string, List<WeightedEntryWithTerm<TKey>>> tokenMatchDataForEntry;
                    if (!allMatchesByKey.TryGetValue(match.Key, out tokenMatchDataForEntry))
                    {
						tokenMatchDataForEntry = new Dictionary<string, List<WeightedEntryWithTerm<TKey>>>(index.TokenComparer);
                        allMatchesByKey.Add(match.Key, tokenMatchDataForEntry);
                    }

                    // Initialise / retrieve data for the current token for the particular match by key
                    if (!tokenMatchDataForEntry.ContainsKey(weightAdjustedToken.Token))
						tokenMatchDataForEntry.Add(weightAdjustedToken.Token, new List<WeightedEntryWithTerm<TKey>>());

                    // Add the new weight value
                    tokenMatchDataForEntry[weightAdjustedToken.Token].Add(
						new WeightedEntryWithTerm<TKey>(
							match.Key,
							match.Weight * weightAdjustedToken.WeightMultiplier,
							match.SourceLocations.Select(l => new SourceFieldLocationWithTerm(
								l.SourceFieldIndex,
								l.TokenIndex,
								l.SourceIndex,
								l.SourceTokenLength,
								weightAdjustedToken.Token
							)).ToNonNullImmutableList()
						)
					);
                }
            }

            // Combine the data for each key
            var allTokens = new NonNullOrEmptyStringList(
                weightAdjustedTokens.Select(t => t.Token).Distinct(index.TokenComparer)
            );
			var combinedData = new NonNullImmutableList<WeightedEntryWithTerm<TKey>>();
            foreach (var match in allMatchesByKey)
            {
				// Each pass through this loop will contain data for a single result key

				// Extract from the data a set of WeightedTokenMatch instances for the current result; get tokens and the match weights for the token for the current result key
				var weightedTokenMatches = match.Value
					.Select(matchSourcesWithToken => new WeightedTokenMatch(
						matchSourcesWithToken.Key, // This is the matched token
						matchSourcesWithToken.Value.Select(v => v.Weight).ToImmutableList()) // This is the set of match weights for the token for a particular result key
					);
				
				// The matches weights for each token must now be combined - if a weight of zero is returned then the match should be ignored
				// - If all tokens are matched for this entry then the number of number of match.Value entries will be the same as the number of distinct broken tokens since
				//   the same string comparer / normaliser is used by the GetMatches calls
				var weight = matchCombiner(
					weightedTokenMatches.ToNonNullImmutableList(),
                    allTokens
                );
                if (weight < 0)
                    throw new Exception("matchCombiner returned negative weight - invalid");
				else if (weight > 0)
				{
					var matchSourceLocations = match.Value.SelectMany(
						matchSourcesWithToken => matchSourcesWithToken.Value.SelectMany(v => v.SourceLocations)
					);
					combinedData = combinedData.Add(new WeightedEntryWithTerm<TKey>(
						match.Key,
						weight,
						matchSourceLocations.ToNonNullImmutableList()
					));
				}
            }
            return combinedData;
        }

        /// <summary>
        /// This will never be called with either null or empty lists. It must always return a value or zero or greater, if zero is returned then the match will excluded from
        /// final resultset.
        /// </summary>
        public delegate float MatchCombiner(NonNullImmutableList<WeightedTokenMatch> matchedTokens, NonNullOrEmptyStringList allTokens);

        public class WeightedTokenMatch
        {
            public WeightedTokenMatch(string matchedToken, ImmutableList<float> weights)
            {
                if (string.IsNullOrWhiteSpace(matchedToken))
                    throw new ArgumentException("Null or empty token specified");
                if (weights == null)
                    throw new ArgumentNullException("weights");
                if (weights.Count == 0)
                    throw new ArgumentException("weight may not be an empty list");
                if (weights.Any(w => w <= 0))
                    throw new ArgumentException("weights may not contain any values of zero or less");

                MatchedToken = matchedToken;
                Weights = weights;
            }

            /// <summary>
            /// This will never be null or empty
            /// </summary>
            public string MatchedToken { get; private set; }

            /// <summary>
            /// This will always never be null or empty and all values will be greater than zero
            /// </summary>
            public ImmutableList<float> Weights { get; private set; }
        }
    }
}
