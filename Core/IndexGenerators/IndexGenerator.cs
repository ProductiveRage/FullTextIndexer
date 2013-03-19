﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FullTextIndexer.Common.Lists;
using FullTextIndexer.Common.Logging;
using FullTextIndexer.Core.Indexes;
using FullTextIndexer.Core.TokenBreaking;
using FullTextIndexer.Core.Indexes.TernarySearchTree;

namespace FullTextIndexer.Core.IndexGenerators
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
        private IndexGenerator.WeightedEntryCombiner _weightedEntryCombiner;
        private ILogger _logger;
        public IndexGenerator(
            NonNullImmutableList<ContentRetriever<TSource, TKey>> contentRetrievers,
            IEqualityComparer<TKey> dataKeyComparer,
            IStringNormaliser sourceStringComparer,
            ITokenBreaker tokenBreaker,
            IndexGenerator.WeightedEntryCombiner weightedEntryCombiner,
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
        /// This will never return null. It will throw an exception for null input.
        /// </summary>
        public IIndexData<TKey> Generate(NonNullImmutableList<TSource> data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            // Build up data about token occurences in the data
            // - We'll be using the token values in the indexContent dictionary after they have been normalised by the sourceStringComparer, this means that we
            //   don't need to specify the sourceStringComparer as the comparer for indexContent which may save some work depending upon the implementation of
            //   the sourceStringComparer
            var timer = new Stopwatch();
            timer.Start();
            var indexContent = new Dictionary<string, Dictionary<TKey, List<WeightedEntry<TKey>>>>();
            var timeElapsedForNextUpdateMessage = TimeSpan.FromSeconds(5);
            for (var index = 0; index < data.Count; index++)
            {
                var entry = data[index];
				var sourceFieldIndex = 0;
				foreach (var contentRetriever in _contentRetrievers)
                {
                    PreBrokenContent<TKey> preBrokenContent;
                    try
                    {
                        preBrokenContent = contentRetriever.InitialContentRetriever(entry);
                    }
                    catch (Exception e)
                    {
                        throw new Exception("contentRetriever.InitialContentRetriever threw exception", e);
                    }
					if (preBrokenContent == null)
						throw new Exception("contentRetriever.InitialContentRetriever returned null - this is invalid");

                    if (timer.Elapsed >= timeElapsedForNextUpdateMessage)
                    {
                        _logger.LogIgnoringAnyError(LogLevel.Debug, () => String.Format("Work completed: {0}%", ((index * 100f)/ (float)data.Count).ToString("0.000")));
                        timeElapsedForNextUpdateMessage = timer.Elapsed.Add(TimeSpan.FromSeconds(5));
                    }

					foreach (var contentSection in preBrokenContent.Content)
					{
						foreach (var weightedTokenMatch in _tokenBreaker.Break(contentSection))
						{
							// Strings that are reduced to "" by the normaliser have no meaning (they can't be searched for) and should be ignored
							var normalisedToken = _sourceStringComparer.GetNormalisedString(weightedTokenMatch.Token);
							if (normalisedToken == "")
								continue;

							Dictionary<TKey, List<WeightedEntry<TKey>>> allDataForToken;
							if (!indexContent.TryGetValue(normalisedToken, out allDataForToken))
							{
								allDataForToken = new Dictionary<TKey, List<WeightedEntry<TKey>>>(_dataKeyComparer);
								indexContent.Add(normalisedToken, allDataForToken);
							}

							if (!allDataForToken.ContainsKey(preBrokenContent.Key))
								allDataForToken.Add(preBrokenContent.Key, new List<WeightedEntry<TKey>>());

							// Each WeightedEntry requires a sourceLocation set which specifies a location in a content field - the SourceLocation
							// returned by the Token Breaker has the token index, start point and length but it needs a distinct field index. The
							// index of the current Content Retriever will do fine.
							allDataForToken[preBrokenContent.Key].Add(
								new WeightedEntry<TKey>(
									preBrokenContent.Key,
									contentRetriever.TokenWeightDeterminer(normalisedToken) * weightedTokenMatch.WeightMultiplier,
									(new[]
									{
										new SourceFieldLocation(
											sourceFieldIndex,
											weightedTokenMatch.SourceLocation.TokenIndex,
											weightedTokenMatch.SourceLocation.SourceIndex,
											weightedTokenMatch.SourceLocation.SourceTokenLength
										)
									}).ToNonNullImmutableList()
								)
							);
						}
						
						// This has to be incremented for each content section successfully extracted from the source data, to ensure that each
						// section gets a unique SourceFieldLocation.SourceFieldIndex assigned to it
						sourceFieldIndex++;
					}
				}
            }
            _logger.LogIgnoringAnyError(
                LogLevel.Debug,
                () => String.Format("Time taken to generate initial token data: {0}ms ({1:0.00}ms per item)", timer.ElapsedMilliseconds, (float)timer.ElapsedMilliseconds / (float)data.Count)
            );
            timer.Restart();

            // Combine entries where Token and Key values match (as with the indexContent dictionary, we don't need to specify the sourceStringComparer as the
            // combinedContent dictionary comparer as all values were stored in indexContent after being normalised - this may save some work depending upon
            // the sourceStringComparer implementation)
            var combinedContent = new Dictionary<string, List<WeightedEntry<TKey>>>();
            foreach (var token in indexContent.Keys)
            {
                combinedContent.Add(token, new List<WeightedEntry<TKey>>());
                foreach (var key in indexContent[token].Keys)
                {
                    var matches = indexContent[token][key];
                    combinedContent[token].Add(
                        new WeightedEntry<TKey>(
                            key,
							_weightedEntryCombiner(matches.Select(m => m.Weight).ToImmutableList()),
							matches.SelectMany(m => m.SourceLocations).ToNonNullImmutableList()
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

    public class IndexGenerator
    {
        /// <summary>
        /// This must always return a value greater than zero, it will never be provided a null or empty list of values and none of the values will be zero of less.
        /// </summary>
        public delegate float WeightedEntryCombiner(ImmutableList<float> weightedValues);
    }
}
