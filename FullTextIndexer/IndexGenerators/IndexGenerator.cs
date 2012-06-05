using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Common.Lists;
using Common.Logging;
using FullTextIndexer.Indexes;
using FullTextIndexer.TokenBreaking;
using FullTextIndexer.Indexes.TernarySearchTree;

namespace FullTextIndexer.IndexGenerators
{
    public class IndexGenerator<TSource, TKey> : IIndexGenerator<TSource, TKey> where TSource : class
    {
        /// <summary>
        /// Note: The sourceStringComparer will be used when GetMatches requests are made against the index data, the other references are used only while the index
        /// data is initially generated.
        /// </summary>
        private NonNullImmutableList<ContentRetriever<TSource, TKey>> _contentRetrievers;
        private IEqualityComparer<TKey> _dataKeyComparer;
        private IStringNormaliser _sourceStringComparer;
        private ITokenBreaker _tokenBreaker;
        private WeightedEntryCombiner _weightedEntryCombiner;
        private ILogger _logger;
        public IndexGenerator(
            NonNullImmutableList<ContentRetriever<TSource, TKey>> contentRetrievers,
            IEqualityComparer<TKey> dataKeyComparer,
            IStringNormaliser sourceStringComparer,
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
        /// This must always return a value greater than zero, it will never be provided a null or empty list of values and none of the values will be zero of less.
        /// </summary>
        public delegate float WeightedEntryCombiner(ImmutableList<float> weightedValues);

        /// <summary>
        /// This will never return null. It will throw an exception for null input.
        /// </summary>
        public IIndexData<TKey> Generate(NonNullImmutableList<TSource> data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            // Build up data about token occurences in the data
            var timer = new Stopwatch();
            timer.Start();
            var indexContent = new Dictionary<string, Dictionary<TKey, List<float>>>(
                _sourceStringComparer
            );
            var timeElapsedForNextUpdateMessage = TimeSpan.FromSeconds(5);
            for (var index = 0; index < data.Count; index++)
            {
                var entry = data[index];
                foreach (var contentRetriever in _contentRetrievers)
                {
                    PreBrokenContent<TKey> preBrokenContent;
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

                    if (timer.Elapsed >= timeElapsedForNextUpdateMessage)
                    {
                        _logger.LogIgnoringAnyError(LogLevel.Debug, () => String.Format("Work completed: {0}%", ((index * 100f)/ (float)data.Count).ToString("0.000")));
                        timeElapsedForNextUpdateMessage = timer.Elapsed.Add(TimeSpan.FromSeconds(5));
                    }

                    foreach (var token in _tokenBreaker.Break(preBrokenContent.Content))
                    {
                        // Strings that are reduced to "" by the normaliser have no meaning (they can't be searched for) and should be ignored
                        if (_sourceStringComparer.GetNormalisedString(token) == "")
                            continue;

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
            var combinedContent = new Dictionary<string, List<WeightedEntry<TKey>>>(
                _sourceStringComparer
            );
            foreach (var token in indexContent.Keys)
            {
                combinedContent.Add(token, new List<WeightedEntry<TKey>>());
                foreach (var key in indexContent[token].Keys)
                {
                    var matches = indexContent[token][key];
                    combinedContent[token].Add(
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
                new TernarySearchTreeDictionary<NonNullImmutableList<WeightedEntry<TKey>>>(
                    combinedContent.Select(entry => new KeyValuePair<string, NonNullImmutableList<WeightedEntry<TKey>>>(entry.Key, entry.Value.ToNonNullImmutableList())),
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
    }
}
