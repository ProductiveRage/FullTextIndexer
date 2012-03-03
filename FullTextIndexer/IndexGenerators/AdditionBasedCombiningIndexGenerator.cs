using System;
using System.Collections.Generic;
using System.Linq;
using Common.Lists;

namespace FullTextIndexer.IndexGenerators
{
    public class AdditionBasedCombiningIndexGenerator<TSource, TKey> : IIndexGenerator<TSource, TKey> where TSource : class
    {
        private NonNullImmutableList<IIndexGenerator<TSource, TKey>> _indexGenerators;
        private IEqualityComparer<string> _sourceStringComparer;
        private IEqualityComparer<TKey> _dataKeyComparer;
        public AdditionBasedCombiningIndexGenerator(
            NonNullImmutableList<IIndexGenerator<TSource, TKey>> indexGenerators,
            IEqualityComparer<string> sourceStringComparer,
            IEqualityComparer<TKey> dataKeyComparer)
        {
            if (indexGenerators == null)
                throw new ArgumentNullException("indexGenerators");
            if (indexGenerators.Count == 0)
                throw new ArgumentException("Empty indexGenerators list specified - invalid");
            if (sourceStringComparer == null)
                throw new ArgumentNullException("sourceStringComparer");
            if (dataKeyComparer == null)
                throw new ArgumentNullException("dataKeyComparer");

            _indexGenerators = indexGenerators;
            _sourceStringComparer = sourceStringComparer;
            _dataKeyComparer = dataKeyComparer;
        }

        /// <summary>
        /// This will never return null. It will throw an exception for null input.
        /// </summary>
        public IndexData<TKey> Generate(NonNullImmutableList<TSource> data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            var combinedIndexContent = new Dictionary<string, List<WeightedEntry<TKey>>>(
                _sourceStringComparer
            );
            foreach (var index in _indexGenerators.Select(g => g.Generate(data)))
            {
                foreach (var token in index.GetAllTokens())
                {
                    foreach (var match in index.GetMatches(token))
                    {
                        if (!combinedIndexContent.ContainsKey(token))
                            combinedIndexContent.Add(token, new List<WeightedEntry<TKey>>());

                        var weightEntriesForToken = combinedIndexContent[token];
                        var weightedEntryForKeyAgainstToken = weightEntriesForToken.FirstOrDefault(e => _dataKeyComparer.Equals(e.Key, match.Key));
                        if (weightedEntryForKeyAgainstToken == null)
                        {
                            // If there is no entry yet for this Token/Key combination then add it to the list and move on
                            weightEntriesForToken.Add(match);
                            continue;
                        }

                        // Otherwise, remove the existing entry and replace it with one that combine it with the current match
                        weightEntriesForToken.Remove(weightedEntryForKeyAgainstToken);
                        weightEntriesForToken.Add(new WeightedEntry<TKey>(
                            weightedEntryForKeyAgainstToken.Key,
                            weightedEntryForKeyAgainstToken.Weight + match.Weight
                        ));
                    }
                }
            }
            return new IndexData<TKey>(
                combinedIndexContent.Select(
                    tokenData => new KeyValuePair<string, IEnumerable<WeightedEntry<TKey>>>(tokenData.Key, tokenData.Value)
                ),
                _sourceStringComparer,
                _dataKeyComparer
            );
        }
    }
}
