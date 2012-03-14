using System;
using System.Collections.Generic;
using System.Linq;
using Common.Lists;

namespace FullTextIndexer.Indexes
{
    public static class IndexData_CombinationExtensions
    {
        /// <summary>
        /// This will return a new IndexData instance that combines the source instance's data with the data other IndexData instances using the specified weight combiner.
        /// In a case where there are different TokenComparer implementations on this instance and on dataToAdd, the comparer from the current instance will be used. It
        /// is recommended that a consistent TokenComparer be used at all times. An exception will be thrown for null dataToAdd or weightCombiner references.
        /// </summary>
        public static IndexData<TKey> Combine<TKey>(this IndexData<TKey> index, NonNullImmutableList<IndexData<TKey>> indexesToAdd, Func<float, float, float> weightCombiner)
        {
            if (indexesToAdd == null)
                throw new ArgumentNullException("indexesToAdd");
            if (weightCombiner == null)
                throw new ArgumentNullException("weightCombiner");

            // Start with a copy of the data in this instance
            var combinedIndexContent = new Dictionary<string, NonNullImmutableList<WeightedEntry<TKey>>>(
                index.TokenComparer
            );
            foreach (var token in index.GetAllTokens())
                combinedIndexContent.Add(token, index.GetMatches(token));

            // Combine with the new data
            foreach (var indexToAdd in indexesToAdd)
            {
                foreach (var token in indexToAdd.GetAllTokens())
                {
                    foreach (var match in indexToAdd.GetMatches(token))
                    {
                        if (!combinedIndexContent.ContainsKey(token))
                            combinedIndexContent.Add(token, new NonNullImmutableList<WeightedEntry<TKey>>());

                        var weightedEntryForKeyAgainstToken = combinedIndexContent[token].FirstOrDefault(e => index.KeyComparer.Equals(e.Key, match.Key));
                        if (weightedEntryForKeyAgainstToken == null)
                        {
                            // If there is no entry yet for this Token/Key combination then add it to the list and move on
                            combinedIndexContent[token] = combinedIndexContent[token].Add(match);
                            continue;
                        }

                        // Otherwise, remove the existing entry and replace it with one that combine it with the current match
                        combinedIndexContent[token] = combinedIndexContent[token]
                            .Remove(weightedEntryForKeyAgainstToken)
                            .Add(new WeightedEntry<TKey>(
                                weightedEntryForKeyAgainstToken.Key,
                                weightedEntryForKeyAgainstToken.Weight + match.Weight
                            ));
                    }
                }
            }

            // Return a new instance containing the combined data
            return new IndexData<TKey>(
                new ImmutableDictionary<string, NonNullImmutableList<WeightedEntry<TKey>>>(
                    combinedIndexContent,
                    index.TokenComparer
                ),
                index.KeyComparer
            );
        }
    }
}
