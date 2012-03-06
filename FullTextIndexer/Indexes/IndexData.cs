using System;
using System.Collections.Generic;
using System.Linq;
using Common.Lists;

namespace FullTextIndexer.Indexes
{
    [Serializable]
    public class IndexData<TKey>
    {
        private ImmutableDictionary<string, NonNullImmutableList<WeightedEntry<TKey>>> _data;
        private IEqualityComparer<TKey> _dataKeyComparer;
        public IndexData(
            ImmutableDictionary<string, NonNullImmutableList<WeightedEntry<TKey>>> data,
            IEqualityComparer<TKey> dataKeyComparer)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (dataKeyComparer == null)
                throw new ArgumentNullException("dataKeyComparer");

            foreach (var token in data.Keys)
            {
                if (token == "")
                    throw new ArgumentException("Null/blank token encountered in data");
                if (data[token].Count == 0)
                    throw new ArgumentException("Empty key set encountered for token: " + token);
                if (data[token].Count != data[token].Select(o => o.Key).Distinct(dataKeyComparer).Count())
                    throw new ArgumentNullException("Multiple entries for the same key encountered in key set for string: " + token);
            }
            _data = data;
            _dataKeyComparer = dataKeyComparer;
        }

        /// <summary>
        /// This constructor is only to be used by the data-manipulating methods that always initialise with data that doesn't need re-validating
        /// </summary>
        private IndexData() { }

        /// <summary>
        /// This will throw an exception for null or blank input. It will never return null. If there are no matches then an empty list will be returned. There will
        /// be no more than a single OccurrenceCount entry for each key.
        /// </summary>
        public NonNullImmutableList<WeightedEntry<TKey>> GetMatches(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                throw new ArgumentException("Null/blank source specified");

            // Since the data dictionary uses the sourceStringComparer, the lookup here will take that into account (so if a case-insensitive sourceStringComparer
            // was specified, for example, then the casing of the "source" value here will be irrelevant)
            if (!_data.ContainsKey(source))
                return new NonNullImmutableList<WeightedEntry<TKey>>();
            return _data[source];
        }

        /// <summary>
        /// This will return a new IndexData instance that combines the current instance's data with the data of another IndexData instance using the specified weight combiner.
        /// In a case where there are different TokenComparer implementations on this instance and on dataToAdd, the comparer from the current instance will be used. It is
        /// recommended that a consistent TokenComparer be used at all times. An exception will be thrown for null dataToAdd or weightCombiner references.
        /// </summary>
        public IndexData<TKey> Combine(NonNullImmutableList<IndexData<TKey>> indexesToAdd, Func<float, float, float> weightCombiner)
        {
            if (indexesToAdd == null)
                throw new ArgumentNullException("indexesToAdd");
            if (weightCombiner == null)
                throw new ArgumentNullException("weightCombiner");

            // Start with a copy of the data in this instance
            var combinedIndexContent = new Dictionary<string, NonNullImmutableList<WeightedEntry<TKey>>>(
                _data.KeyComparer
            );
            foreach (var token in _data.Keys)
                combinedIndexContent.Add(token, _data[token]);

            // Combine with the new data
            foreach (var index in indexesToAdd)
            {
                foreach (var token in index.GetAllTokens())
                {
                    foreach (var match in index.GetMatches(token))
                    {
                        if (!combinedIndexContent.ContainsKey(token))
                            combinedIndexContent.Add(token, new NonNullImmutableList<WeightedEntry<TKey>>());

                        var weightedEntryForKeyAgainstToken = combinedIndexContent[token].FirstOrDefault(e => _dataKeyComparer.Equals(e.Key, match.Key));
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
                    _data.KeyComparer
                ),
                _dataKeyComparer
            );
        }

        /// <summary>
        /// This will return a new IndexData instance without any data relating to the specified keys. Any keys that are specified to remove that are not present in the
        /// data will be ignored.
        /// </summary>
        public IndexData<TKey> RemoveEntriesFor(ImmutableList<TKey> keysToRemove)
        {
            if (keysToRemove == null)
                throw new ArgumentNullException("keysToRemove");

            var dataNew = new Dictionary<string, NonNullImmutableList<WeightedEntry<TKey>>>(
                _data.KeyComparer
            );
            foreach (var token in _data.Keys)
            {
                var matchesForToken = _data[token];
                var trimmedWeightedEntries = matchesForToken.Where(m => !keysToRemove.Any(k => _dataKeyComparer.Equals(m.Key, k)));
                if (trimmedWeightedEntries.Any())
                    dataNew.Add(token, trimmedWeightedEntries.ToNonNullImmutableList());
            }
            return new IndexData<TKey>()
            {
                _data = new ImmutableDictionary<string, NonNullImmutableList<WeightedEntry<TKey>>>(dataNew, _data.KeyComparer),
                _dataKeyComparer = _dataKeyComparer
            };
        }

        /// <summary>
        /// This will never return null
        /// </summary>
        public NonNullOrEmptyStringList GetAllTokens()
        {
            return new NonNullOrEmptyStringList(_data.Keys);
        }

        /// <summary>
        /// This will never return null
        /// </summary>
        public IEqualityComparer<string> TokenComparer
        {
            get { return _data.KeyComparer; }
        }

        /// <summary>
        /// This will never return null
        /// </summary>
        public IEqualityComparer<TKey> KeyComparer
        {
            get { return _dataKeyComparer; }
        }
    }
}
