using System;
using System.Collections.Generic;
using System.Linq;
using Common.Lists;
using FullTextIndexer.Indexes;
using FullTextIndexer.TokenBreaking;

namespace FullTextIndexer.IndexGenerators
{
    public class IndexGenerator<TSource, TKey> : IIndexGenerator<TSource, TKey> where TSource : class
    {
        /// <summary>
        /// Note: The sourceStringComparer will be used when GetMatches requests are made against the index data, the other references are used only while the index
        /// data is initially generated.
        /// </summary>
        private dataKeyRetriever _dataKeyRetriever;
        private IEqualityComparer<TKey> _dataKeyComparer;
        private SourceRetriever _sourceRetriever;
        private IEqualityComparer<string> _sourceStringComparer;
        private ITokenBreaker _tokenBreaker;
        private WeightDeterminer _weightDeterminer;
        public IndexGenerator(
            dataKeyRetriever dataKeyRetriever,
            IEqualityComparer<TKey> dataKeyComparer,
            SourceRetriever sourceRetriever,
            IEqualityComparer<string> sourceStringComparer,
            ITokenBreaker tokenBreaker,
            WeightDeterminer weightDeterminer)
        {
            if (dataKeyRetriever == null)
                throw new ArgumentNullException("dataKeyRetriever");
            if (dataKeyComparer == null)
                throw new ArgumentNullException("dataKeyComparer");
            if (sourceRetriever == null)
                throw new ArgumentNullException("sourceRetriever");
            if (sourceStringComparer == null)
                throw new ArgumentNullException("sourceStringComparer");
            if (tokenBreaker == null)
                throw new ArgumentNullException("tokenBreaker");
            if (weightDeterminer == null)
                throw new ArgumentNullException("weightDeterminer");

            _dataKeyRetriever = dataKeyRetriever;
            _dataKeyComparer = dataKeyComparer;
            _sourceRetriever = sourceRetriever;
            _sourceStringComparer = sourceStringComparer;
            _tokenBreaker = tokenBreaker;
            _weightDeterminer = weightDeterminer;
        }

        /// <summary>
        /// This must return a key that uniquely identifies that source. It must never return null. It will never be provided a null source value.
        /// </summary>
        public delegate TKey dataKeyRetriever(TSource source);

        /// <summary>
        /// This must return a string to examine for the specified source. If there is no data on the source, then empty string should be returned, not null.
        /// It will never be provided a null source value.
        /// </summary>
        public delegate string SourceRetriever(TSource source);

        /// <summary>
        /// This must always return a value greater than zero, it will never be provided a null or empty token nor an occurenceCount less than one.
        /// </summary>
        public delegate float WeightDeterminer(string token, int occurenceCount);

        /// <summary>
        /// This will never return null. It will throw an exception for null input.
        /// </summary>
        public IndexData<TKey> Generate(NonNullImmutableList<TSource> data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            // Build up data about token occurences in the data
            var indexContent = new Dictionary<string, List<OccurenceCount>>(
                _sourceStringComparer
            );
            foreach (var entry in data)
            {
                TKey key;
                try
                {
                    key = _dataKeyRetriever(entry);
                }
                catch(Exception e)
                {
                    throw new Exception("dataKeyRetriever threw exception", e);
                }
                if (key == null)
                    throw new Exception("dataKeyRetriever returned null");

                string sourceString;
                try
                {
                    sourceString = _sourceRetriever(entry);
                }
                catch(Exception e)
                {
                    throw new Exception("sourceRetriever threw exception", e);
                }
                if (sourceString == null)
                    throw new Exception("sourceRetriever returned null");

                foreach (var token in _tokenBreaker.Break(sourceString))
                {
                    if (!indexContent.ContainsKey(token))
                        indexContent.Add(token, new List<OccurenceCount>());
                    
                    var occurenceDataForToken = indexContent[token];
                    var occurenceEntryForToken = occurenceDataForToken.FirstOrDefault(o => _dataKeyComparer.Equals(o.Key, key));
                    if (occurenceEntryForToken == null)
                        occurenceDataForToken.Add(new OccurenceCount(key));
                    else
                        occurenceEntryForToken.IncreaseCount();
                }
            }

            // Translate this into an IndexData instance
            var indexData = new Dictionary<string, NonNullImmutableList<WeightedEntry<TKey>>>(
                _sourceStringComparer
            );
            foreach (var tokenData in indexContent)
                indexData.Add(tokenData.Key, GetWeightedEntries(tokenData.Key, tokenData.Value));
            return new IndexData<TKey>(
                new ImmutableDictionary<string, NonNullImmutableList<WeightedEntry<TKey>>>(
                    indexData,
                    _sourceStringComparer
                ),
                _dataKeyComparer
            );
        }

        private NonNullImmutableList<WeightedEntry<TKey>> GetWeightedEntries(string token, List<OccurenceCount> occurences)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Null/empty token specified");
            if (occurences == null)
                throw new ArgumentNullException("occurences");

            var weightedEntries = new List<WeightedEntry<TKey>>();
            foreach (var occurence in occurences)
            {
                if (occurence == null)
                    throw new ArgumentException("Null entry encountered in occurences");
                weightedEntries.Add(
                    new WeightedEntry<TKey>(occurence.Key, _weightDeterminer(token, occurence.Count))
                );
            }
            return weightedEntries.ToNonNullImmutableList();
        }

        private class OccurenceCount
        {
            public OccurenceCount(TKey key)
            {
                if (key == null)
                    throw new ArgumentNullException("key");

                Key = key;
                Count = 1;
            }

            /// <summary>
            /// This will never be null
            /// </summary>
            public TKey Key { get; private set; }

            /// <summary>
            /// This will always be greater than zero
            /// </summary>
            public int Count { get; private set; }

            public void IncreaseCount()
            {
                Count++;
            }
        }
    }
}
