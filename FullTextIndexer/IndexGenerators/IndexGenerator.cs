using System;
using System.Collections.Generic;
using System.Diagnostics;
using Common.Lists;
using Common.Logging;
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
        private NonNullImmutableList<ContentRetriever> _contentRetrievers;
        private IEqualityComparer<TKey> _dataKeyComparer;
        private IEqualityComparer<string> _sourceStringComparer;
        private ITokenBreaker _tokenBreaker;
        private WeightedEntryCombiner _weightedEntryCombiner;
        private ILogger _logger;
        public IndexGenerator(
            NonNullImmutableList<ContentRetriever> contentRetrievers,
            IEqualityComparer<TKey> dataKeyComparer,
            IEqualityComparer<string> sourceStringComparer,
            ITokenBreaker tokenBreaker,
            WeightedEntryCombiner weightedEntryCombiner,
            ILogger logger)
        {
            if (contentRetrievers == null)
                throw new ArgumentNullException("contentRetrievers");
            if (dataKeyComparer == null)
                throw new ArgumentNullException("dataKeyComparer");
            if (sourceStringComparer == null)
                throw new ArgumentNullException("sourceStringComparer");
            if (tokenBreaker == null)
                throw new ArgumentNullException("tokenBreaker");
            if (weightedEntryCombiner == null)
                throw new ArgumentNullException("weightedEntryCombiner");
            if (logger == null)
                throw new ArgumentNullException("logger");

            _contentRetrievers = contentRetrievers;
            _dataKeyComparer = dataKeyComparer;
            _sourceStringComparer = sourceStringComparer;
            _tokenBreaker = tokenBreaker;
            _weightedEntryCombiner = weightedEntryCombiner;
            _logger = logger;
        }

        /// <summary>
        /// This will never be provided a null source value. If the content retriever does not identify any content it is valid to return null.
        /// </summary>
        public delegate PreBrokenContent PreBrokenTokenContentRetriever(TSource source);

        /// <summary>
        /// This must always return a value greater than zero, it will never be provided a null or empty token.
        /// </summary>
        public delegate float BrokenTokenWeightDeterminer(string token);

        /// <summary>
        /// This must always return a value greater than zero, it will never be provided a null or empty list of values and none of the values will be zero of less.
        /// </summary>
        public delegate float WeightedEntryCombiner(ImmutableList<float> weightedValues);

        /// <summary>
        /// This will never return null. It will throw an exception for null input.
        /// </summary>
        public IndexData<TKey> Generate(NonNullImmutableList<TSource> data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            // Build up data about token occurences in the data
            var timer = new Stopwatch();
            timer.Start();
            var indexContent = new Dictionary<string, Dictionary<TKey, List<float>>>(
                _sourceStringComparer
            );
            foreach (var entry in data)
            {
                foreach (var contentRetriever in _contentRetrievers)
                {
                    PreBrokenContent preBrokenContent;
                    try
                    {
                        preBrokenContent = contentRetriever.InitialContentRetriever(entry);
                        if (preBrokenContent == null)
                        {
                            // If no content is returned (which is valid, depending upon the input data and the content retriever), then move on
                            continue;
                        }
                    }
                    catch (Exception e)
                    {
                        throw new Exception("contentRetriever.InitialContentRetriever threw exception", e);
                    }

                    foreach (var token in _tokenBreaker.Break(preBrokenContent.Content))
                    {
                        if (!indexContent.ContainsKey(token))
                            indexContent.Add(token, new Dictionary<TKey, List<float>>(_dataKeyComparer));

                        var allDataForToken = indexContent[token];
                        if (!allDataForToken.ContainsKey(preBrokenContent.Key))
                            allDataForToken.Add(preBrokenContent.Key, new List<float>());

                        allDataForToken[preBrokenContent.Key].Add(
                            contentRetriever.TokenWeightDeterminer(token)
                        );
                    }
                }
            }
            _logger.LogIgnoringAnyError(
                LogLevel.Debug,
                () => String.Format("Time taken to generate initial token data: {0}ms ({1:0.00}ms per item)", timer.ElapsedMilliseconds, (float)timer.ElapsedMilliseconds / (float)data.Count)
            );
            timer.Restart();

            // Combine entries where Token and Key values match
            var combinedContent = new Dictionary<string, NonNullImmutableList<WeightedEntry<TKey>>>(
                _sourceStringComparer
            );
            foreach (var token in indexContent.Keys)
            {
                combinedContent.Add(token, new NonNullImmutableList<WeightedEntry<TKey>>());
                foreach (var key in indexContent[token].Keys)
                {
                    var matches = indexContent[token][key];
                    combinedContent[token] = combinedContent[token].Add(
                        new WeightedEntry<TKey>(
                            key,
                            _weightedEntryCombiner(matches.ToImmutableList())
                        )
                    );
                }
            }
            _logger.LogIgnoringAnyError(
                LogLevel.Debug,
                () => String.Format("Time taken to combine token data sets: {0}ms ({1:0.00}ms per item)", timer.ElapsedMilliseconds, (float)timer.ElapsedMilliseconds / (float)data.Count)
            );
            timer.Restart();

            // Translate this into an IndexData instance
            var indexData = new IndexData<TKey>(
                new ImmutableDictionary<string, NonNullImmutableList<WeightedEntry<TKey>>>(
                    combinedContent,
                    _sourceStringComparer
                ),
                _dataKeyComparer
            );
            _logger.LogIgnoringAnyError(
                LogLevel.Debug,
                () => String.Format("Time taken to generate final IndexData: {0}ms ({1:0.00}ms per item)", timer.ElapsedMilliseconds, (float)timer.ElapsedMilliseconds / (float)data.Count)
            );
            return indexData;
        }

        public class PreBrokenContent
        {
            public PreBrokenContent(TKey key, string content)
            {
                if (key == null)
                    throw new ArgumentNullException("key");
                if (string.IsNullOrWhiteSpace(content))
                    throw new ArgumentException("Null/blank content specified");

                Key = key;
                Content = content;
            }

            /// <summary>
            /// This will never be null
            /// </summary>
            public TKey Key { get; private set; }

            /// <summary>
            /// This will never be null or empty
            /// </summary>
            public string Content { get; private set; }
        }

        public class ContentRetriever
        {
            public ContentRetriever(PreBrokenTokenContentRetriever initialContentRetriever, BrokenTokenWeightDeterminer tokenWeightDeterminer)
            {
                if (initialContentRetriever == null)
                    throw new ArgumentNullException("initialContentRetriever");
                if (tokenWeightDeterminer == null)
                    throw new ArgumentNullException("tokenWeightDeterminer");

                InitialContentRetriever = initialContentRetriever;
                TokenWeightDeterminer = tokenWeightDeterminer;
            }

            /// <summary>
            /// This will never be null
            /// </summary>
            public PreBrokenTokenContentRetriever InitialContentRetriever { get; private set; }

            /// <summary>
            /// This will never be null
            /// </summary>
            public BrokenTokenWeightDeterminer TokenWeightDeterminer { get; private set; }
        }
    }
}
