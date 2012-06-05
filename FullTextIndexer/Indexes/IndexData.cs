using System;
using System.Collections.Generic;
using System.Linq;
using Common.Lists;

namespace FullTextIndexer.Indexes
{
    [Serializable]
    public class IndexData<TKey> : IIndexData<TKey>
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
        /// This will throw an exception for null or blank input. It will never return null. If there are no matches then an empty list will be returned.
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
        /// This will return a new IndexData instance that combines the source instance's data with the data other IndexData instances using the specified weight combiner.
        /// In a case where there are different TokenComparer implementations on this instance and on dataToAdd, the comparer from the current instance will be used. It
        /// is recommended that a consistent TokenComparer be used at all times. An exception will be thrown for null dataToAdd or weightCombiner references.
        /// </summary>
        public IIndexData<TKey> Combine(NonNullImmutableList<IIndexData<TKey>> indexesToAdd, Func<float, float, float> weightCombiner)
        {
            if (indexesToAdd == null)
                throw new ArgumentNullException("indexesToAdd");
            if (weightCombiner == null)
                throw new ArgumentNullException("weightCombiner");

            // Start with a copy of the data in this instance
            var combinedIndexContent = new Dictionary<string, NonNullImmutableList<WeightedEntry<TKey>>>(
                TokenComparer
            );
            foreach (var token in GetAllTokens())
                combinedIndexContent.Add(token, GetMatches(token));

            // Combine with the new data
            foreach (var indexToAdd in indexesToAdd)
            {
                foreach (var token in indexToAdd.GetAllTokens())
                {
                    foreach (var match in indexToAdd.GetMatches(token))
                    {
                        if (!combinedIndexContent.ContainsKey(token))
                            combinedIndexContent.Add(token, new NonNullImmutableList<WeightedEntry<TKey>>());

                        var weightedEntryForKeyAgainstToken = combinedIndexContent[token].FirstOrDefault(e => KeyComparer.Equals(e.Key, match.Key));
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
                    TokenComparer
                ),
                KeyComparer
            );
        }

        /// <summary>
        /// This will return a new IndexData instance without any data relating to keys identified by the removeIf filter
        /// </summary>
        public IndexData<TKey> Remove(Predicate<TKey> removeIf)
        {
            if (removeIf == null)
                throw new ArgumentNullException("removeIf");

            var dataNew = new Dictionary<string, NonNullImmutableList<WeightedEntry<TKey>>>(
                TokenComparer
            );
            foreach (var token in GetAllTokens())
            {
                var matchesForToken = GetMatches(token);
                var trimmedWeightedEntries = matchesForToken.Where(m => !removeIf(m.Key));
                if (trimmedWeightedEntries.Any())
                    dataNew.Add(token, trimmedWeightedEntries.ToNonNullImmutableList());
            }

            // Return a new instance containing the combined data
            return new IndexData<TKey>(
                new ImmutableDictionary<string, NonNullImmutableList<WeightedEntry<TKey>>>(
                    dataNew,
                    TokenComparer
                ),
                KeyComparer
            );
        }

        /// <summary>
        /// This will never return null, the returned dictionary will have this instance's KeyNormaliser as its comparer
        /// </summary>
        public IDictionary<string, NonNullImmutableList<WeightedEntry<TKey>>> ToDictionary()
        {
            var dictionary = new Dictionary<string, NonNullImmutableList<WeightedEntry<TKey>>>(
                _data.KeyComparer
            );
            foreach (var key in _data.Keys)
                dictionary.Add(key, _data[key]);
            return dictionary;
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
