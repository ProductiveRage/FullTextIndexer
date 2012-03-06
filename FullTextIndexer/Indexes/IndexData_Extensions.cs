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
        /// This GetMatches signature will break a given source string and return results based upon the combination of partial matches - the token breaker and the match combining
        /// are specified by the caller
        /// </summary>
        public static NonNullImmutableList<WeightedEntry<TKey>> GetMatches<TKey>(
            this IndexData<TKey> index,
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
                    allMatchesByKey[match.Key].Add(new WeightedTokenMatch(tokens, token, match.Weight));
                }
            }

            var combinedData = new List<WeightedEntry<TKey>>();
            foreach (var match in allMatchesByKey)
            {
                var weight = matchCombiner(match.Value.ToNonNullImmutableList());
                if (weight <= 0)
                    throw new Exception("matchCombiner return weight of zero or less - invalid");
                combinedData.Add(new WeightedEntry<TKey>(match.Key, weight));
            }
            return combinedData.ToNonNullImmutableList();
        }

        /// <summary>
        /// This will never be called with a null or empty list. It must always return a value greater than zero.
        /// </summary>
        public delegate float MatchCombiner(NonNullImmutableList<WeightedTokenMatch> tokenMatches);

        public class WeightedTokenMatch
        {
            public WeightedTokenMatch(NonNullOrEmptyStringList allTokens, string matchedToken, float weight)
            {
                if (allTokens == null)
                    throw new ArgumentNullException("allTokens");
                if (string.IsNullOrWhiteSpace(matchedToken))
                    throw new ArgumentException("Null or empty token specified");
                if (weight <= 0)
                    throw new ArgumentOutOfRangeException("weight", "must be > 0");

                AllTokens = allTokens;
                MatchedToken = matchedToken;
                Weight = weight;
            }

            /// <summary>
            /// This will never be null or empty
            /// </summary>
            public NonNullOrEmptyStringList AllTokens { get; private set; }

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
