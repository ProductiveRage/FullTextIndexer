using System;
using System.Collections.Generic;
using System.Linq;
using Common.Lists;
using FullTextIndexer.TokenBreaking;

namespace FullTextIndexer.Indexes
{
    public static class IndexData_Extensions
    {
        /// <summary>
        /// This will break a given source string and return results based upon the combination of partial matches (so results that only match part of the source string may be included
        /// in the returned data). The token breaker and the match combiner must be specified by the caller - if the match combiner returns zero then the result will not be included in
        /// the final data. To require that all tokens in the source content be present for any returned results, the following matchCombiner could be specified:
        ///  (tokenMatches, allTokens) => (tokenMatches.Count < allTokens.Count) ? 0 : tokenMatches.Sum(m => m.Weight)
        /// </summary>
        public static NonNullImmutableList<WeightedEntry<TKey>> GetPartialMatches<TKey>(
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
            var allMatchesByKey = new Dictionary<TKey, Dictionary<string, List<float>>>(
                index.KeyComparer
            );
            var weightAdjustedTokens = tokenBreaker.Break(source);
            foreach (var weightAdjustedToken in weightAdjustedTokens)
            {
                foreach (var match in index.GetMatches(weightAdjustedToken.Token))
                {
                    // Initialise / retrieve all token data for particular match by key
                    Dictionary<string, List<float>> tokenMatchDataForEntry;
                    if (!allMatchesByKey.TryGetValue(match.Key, out tokenMatchDataForEntry))
                    {
                        tokenMatchDataForEntry = new Dictionary<string, List<float>>(index.TokenComparer);
                        allMatchesByKey.Add(match.Key, tokenMatchDataForEntry);
                    }

                    // Initialise / retrieve data for the current token for the particular match by key
                    if (!tokenMatchDataForEntry.ContainsKey(weightAdjustedToken.Token))
                        tokenMatchDataForEntry.Add(weightAdjustedToken.Token, new List<float>());

                    // Add the new weight value
                    tokenMatchDataForEntry[weightAdjustedToken.Token].Add(match.Weight * weightAdjustedToken.WeightMultiplier);
                }
            }

            // Combine the data for each key
            var allTokens = new NonNullOrEmptyStringList(
                weightAdjustedTokens.Select(t => t.Token).Distinct(index.TokenComparer)
            );
            var combinedData = new List<WeightedEntry<TKey>>();
            foreach (var match in allMatchesByKey)
            {
                // If a weight of zero is returned then the match should be ignored
                // - If all tokens are matched for this entry then the number of number of match.Value entries will be the same as the number of distinct broken tokens since
                //   the same string comparer / normaliser is used by the GetMatches calls 
                var weight = matchCombiner(
                    match.Value.Select(v => new WeightedTokenMatch(v.Key, v.Value.ToImmutableList())).ToNonNullImmutableList(),
                    allTokens
                );
                if (weight < 0)
                    throw new Exception("matchCombiner returned negative weight - invalid");
                else if (weight > 0)
                    combinedData.Add(new WeightedEntry<TKey>(match.Key, weight));
            }
            return combinedData.ToNonNullImmutableList();
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
