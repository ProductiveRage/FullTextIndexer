using System;
using System.Collections.Generic;
using System.Linq;
using Common.Lists;

namespace FullTextIndexer.IndexGenerators
{
    [Serializable]
    public class IndexData<TKey>
    {
        private Dictionary<string, NonNullImmutableList<WeightedEntry<TKey>>> _data;
        private IEqualityComparer<string> _sourceStringComparer;
        private IEqualityComparer<TKey> _dataKeyComparer;
        public IndexData(
            IEnumerable<KeyValuePair<string, IEnumerable<WeightedEntry<TKey>>>> data,
            IEqualityComparer<string> sourceStringComparer,
            IEqualityComparer<TKey> dataKeyComparer)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (sourceStringComparer == null)
                throw new ArgumentNullException("sourceStringComparer");
            if (dataKeyComparer == null)
                throw new ArgumentNullException("dataKeyComparer");

            var dataTidied = new Dictionary<string, NonNullImmutableList<WeightedEntry<TKey>>>(
                sourceStringComparer
            );
            foreach (var entry in data)
            {
                // Ensure source string is valid (not null, empty or a duplicate)
                if (string.IsNullOrWhiteSpace(entry.Key))
                    throw new ArgumentException("Null/blank string encountered in data");
                var sourceString = entry.Key;
                if (dataTidied.ContainsKey(sourceString))
                    throw new ArgumentException("Duplicate string encountered in data: " + sourceString);
                
                // Ensure occurences set is valid (not null, no null entries, no duplicated key values)
                var occurences = entry.Value;
                if (occurences == null)
                    throw new ArgumentException("Null occurences set encountered in data for string: " + sourceString);
                NonNullImmutableList<WeightedEntry<TKey>> occurencesList;
                try
                {
                    occurencesList = occurences.ToNonNullImmutableList();
                }
                catch (ArgumentException e)
                {
                    throw new ArgumentException("Null entry encountered in key set for string: " + sourceString, e);
                }
                if (occurencesList.Count == 0)
                    throw new ArgumentException("Empty key set encountered for string: " + sourceString);
                if (occurencesList.Count != occurencesList.Select(o => o.Key).Distinct(dataKeyComparer).Count())
                    throw new ArgumentNullException("Multiple entries for the same key encountered in key set for string: " + sourceString);

                dataTidied.Add(sourceString, occurencesList);
            }
            _data = dataTidied;
            _sourceStringComparer = sourceStringComparer;
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
        public IndexData<TKey> Combine(IndexData<TKey> dataToAdd, Func<float, float, float> weightCombiner)
        {
            if (dataToAdd == null)
                throw new ArgumentNullException("dataToAdd");
            if (weightCombiner == null)
                throw new ArgumentNullException("weightCombiner");

            // Start with a copy of the data in this instance
            var combinedIndexContent = new Dictionary<string, List<WeightedEntry<TKey>>>(
                _sourceStringComparer
            );
            foreach (var entry in _data)
                combinedIndexContent.Add(entry.Key, entry.Value.ToList());

            // Combine with the new data
            foreach (var token in dataToAdd.GetAllTokens())
            {
                foreach (var match in dataToAdd.GetMatches(token))
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
            
            // Return a new instance containing the combined data
            return new IndexData<TKey>(
                combinedIndexContent.Select(
                    tokenData => new KeyValuePair<string, IEnumerable<WeightedEntry<TKey>>>(tokenData.Key, tokenData.Value)
                ),
                _sourceStringComparer,
                _dataKeyComparer
            );
        }

        /// <summary>
        /// This will return a new IndexData instance without any data relating to the specified keys. Any keys that are specified to remove that are not present in the
        /// data will be ignored.
        /// </summary>
        public IndexData<TKey> RemoveEntriesFor(IEnumerable<TKey> keysToRemove)
        {
            if (keysToRemove == null)
                throw new ArgumentNullException("keysToRemove");

            var dataNew = new Dictionary<string, NonNullImmutableList<WeightedEntry<TKey>>>(
                _sourceStringComparer
            );
            foreach (var entry in _data)
            {
                var trimmedWeightedEntries = entry.Value.Where(e => !keysToRemove.Any(k => _dataKeyComparer.Equals(e.Key, k)));
                if (trimmedWeightedEntries.Any())
                    dataNew.Add(entry.Key, trimmedWeightedEntries.ToNonNullImmutableList());
            }
            return new IndexData<TKey>()
            {
                _data = dataNew,
                _sourceStringComparer = _sourceStringComparer,
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
            get { return _sourceStringComparer; }
        }
    }
}
