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
