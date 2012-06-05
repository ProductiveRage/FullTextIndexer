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
            var allMatchesByKey = new Dictionary<TKey, List<WeightedTokenMatch>>(
                index.KeyComparer
            );
            var tokens = new NonNullOrEmptyStringList(
                tokenBreaker.Break(source).Distinct(index.TokenComparer)
            );
            foreach (var token in tokens)
            {
                foreach (var match in index.GetMatches(token))
                {
                    if (!allMatchesByKey.ContainsKey(match.Key))
                        allMatchesByKey.Add(match.Key, new List<WeightedTokenMatch>());
                    allMatchesByKey[match.Key].Add(new WeightedTokenMatch(token, match.Weight));
                }
            }

            // Combine the data for each key, if AllBrokenTokensMustBeMatched is specified then skip over any keys that don't match all of the tokens
            var combinedData = new List<WeightedEntry<TKey>>();
            foreach (var match in allMatchesByKey)
            {
                // If a weight of zero is returned then the match should be ignored
                var weight = matchCombiner(match.Value.ToNonNullImmutableList(), tokens);
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
        public delegate float MatchCombiner(NonNullImmutableList<WeightedTokenMatch> tokenMatches, NonNullOrEmptyStringList allTokens);

        public class WeightedTokenMatch
        {
            public WeightedTokenMatch(string matchedToken, float weight)
            {
                if (string.IsNullOrWhiteSpace(matchedToken))
                    throw new ArgumentException("Null or empty token specified");
                if (weight <= 0)
                    throw new ArgumentOutOfRangeException("weight", "must be > 0");

                MatchedToken = matchedToken;
                Weight = weight;
            }

            /// <summary>
            /// This will never be null or empty
            /// </summary>
            public string MatchedToken { get; private set; }

            /// <summary>
            /// This will always be greater than zero
            /// </summary>
            public float Weight { get; private set; }
        }
    }
}
