using System;
using System.Collections.Generic;
using FullTextIndexer.Common.Lists;

namespace FullTextIndexer.Core.Indexes
{
    public static class WeightedEntry_Extensions
    {
        /// <summary>
        /// Combine multiple sets of WeighedEntries together based upon the specified matchCombiner. This will throw an exception for any null argument.
        /// </summary>
        public static NonNullImmutableList<WeightedEntry<TKey>> CombineResults<TKey>(
            this NonNullImmutableList<WeightedEntry<TKey>> results,
            NonNullImmutableList<NonNullImmutableList<WeightedEntry<TKey>>> resultSetsToAdd,
            IEqualityComparer<TKey> keyComparer,
            MatchCombiner matchCombiner)
        {
            if (results == null)
                throw new ArgumentNullException("results");
            if (resultSetsToAdd == null)
                throw new ArgumentNullException("resultSetsToAdd");
            if (keyComparer == null)
                throw new ArgumentNullException("keyComparer");
            if (matchCombiner == null)
                throw new ArgumentNullException("matchCombiner");

            var allMatchesByKey = new Dictionary<TKey, ImmutableList<float>>(
                keyComparer
            );
            foreach (var resultSet in resultSetsToAdd.Add(results))
            {
                foreach (var result in resultSet)
                {
					if (!allMatchesByKey.ContainsKey(result.Key))
						allMatchesByKey.Add(result.Key, new ImmutableList<float>());
					allMatchesByKey[result.Key] = allMatchesByKey[result.Key].Add(result.Weight);
                }
            }

            var combinedData = new List<WeightedEntry<TKey>>();
            foreach (var match in allMatchesByKey)
            {
                var weight = matchCombiner(match.Value);
                if (weight <= 0)
                    throw new Exception("matchCombiner return weight of zero or less - invalid");
                combinedData.Add(new WeightedEntry<TKey>(match.Key, weight));
            }
            return combinedData.ToNonNullImmutableList();
        }

        /// <summary>
        /// Combine two sets of WeighedEntries together based upon the specified matchCombiner. This will throw an exception for any null argument.
        /// </summary>
        public static NonNullImmutableList<WeightedEntry<TKey>> CombineResults<TKey>(
            this NonNullImmutableList<WeightedEntry<TKey>> results,
            NonNullImmutableList<WeightedEntry<TKey>> resultsToAdd,
            IEqualityComparer<TKey> keyComparer,
            MatchCombiner matchCombiner)
        {
            if (resultsToAdd == null)
                throw new ArgumentNullException("resultsToAdd");
            if (keyComparer == null)
                throw new ArgumentNullException("keyComparer");
            if (matchCombiner == null)
                throw new ArgumentNullException("matchCombiner");

            return CombineResults(
                results,
                (new[] { resultsToAdd }).ToNonNullImmutableList(),
                keyComparer,
                matchCombiner
            );
        }

        /// <summary>
        /// This will never be called with a null or empty list. It must always return a value greater than zero.
        /// </summary>
        public delegate float MatchCombiner(ImmutableList<float> matchWeights);
    }
}
