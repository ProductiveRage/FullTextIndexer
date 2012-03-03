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
                var sourceString = entry.Key.Trim();
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
        }

        /// <summary>
        /// This will throw an exception for null or blank input. It will never return null. If there are no matches then an empty list will be returned. There will
        /// be no more than a single OccurrenceCount entry for each key.
        /// </summary>
        public NonNullImmutableList<WeightedEntry<TKey>> GetMatches(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                throw new ArgumentException("Null/blank source specified");

            if (!_data.ContainsKey(source))
                return new NonNullImmutableList<WeightedEntry<TKey>>();
            return _data[source];
        }
    }
}
