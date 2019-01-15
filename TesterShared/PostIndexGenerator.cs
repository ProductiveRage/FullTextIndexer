using System;
using System.Collections.Generic;
using System.Linq;
using FullTextIndexer.Common.Lists;
using FullTextIndexer.Common.Logging;
using FullTextIndexer.Core;
using FullTextIndexer.Core.Indexes;
using FullTextIndexer.Core.Indexes.TernarySearchTree;
using FullTextIndexer.Core.IndexGenerators;
using FullTextIndexer.Core.TokenBreaking;
using Tester.SourceData;

namespace Tester
{
	public sealed class PostIndexGenerator
	{
		private readonly ITokenBreaker _tokenBreaker;
		private readonly IStringNormaliser _sourceStringComparer;
		private readonly HashSet<string> _stopWordLookup;
		private readonly ILogger _logger;
		public PostIndexGenerator(ITokenBreaker tokenBreaker, IStringNormaliser sourceStringComparer, ILogger logger)
		{
			if (tokenBreaker == null)
				throw new ArgumentNullException("tokenBreaker");
			if (sourceStringComparer == null)
				throw new ArgumentNullException("sourceStringComparer");
			if (logger == null)
				throw new ArgumentNullException("logger");

			_tokenBreaker = tokenBreaker;
			_sourceStringComparer = sourceStringComparer;
			_stopWordLookup = new HashSet<string>(Constants.GetStopWords("en"), _sourceStringComparer); // TODO: Explain (if it helps)
			_logger = logger;
		}

		public IndexData<int> Generate(NonNullImmutableList<Post> data)
		{
			if (data == null)
				throw new ArgumentNullException("data");

			var contentRetrievers = new List<ContentRetriever<Post, int>>();

			// Instantiate content retriever for the Title and Tags, these have higher weights assigned to them than the other fields
			contentRetrievers.Add(new ContentRetriever<Post, int>(
				p => new PreBrokenContent<int>(p.Id, p.Title),
				GetTokenWeightDeterminer(15f)
			));
			contentRetrievers.Add(new ContentRetriever<Post, int>(
				p => new PreBrokenContent<int>(p.Id, p.Tags),
				GetTokenWeightDeterminer(3f)
			));

			// Instantiate content retriever for the Body field
			contentRetrievers.Add(new ContentRetriever<Post, int>(
				p => new PreBrokenContent<int>(p.Id, p.Body),
				GetStandardTokenWeightDeterminer()
			));

			// Instantiate content retriever for the Author field (these shouldn't match with as much weight but still makes sense to make it searchable)
			contentRetrievers.Add(new ContentRetriever<Post, int>(
				p => new PreBrokenContent<int>(p.Id, p.Author),
				GetTokenWeightDeterminer(0.1f)
			));

			return
				new IndexGenerator<Post, int>(
					contentRetrievers.ToNonNullImmutableList(),
					new DefaultEqualityComparer<int>(),
					_sourceStringComparer,
					_tokenBreaker,
					weightedValues => weightedValues.Sum(),
					captureSourceLocations: true,
					logger: _logger
				)
				.Generate(data);
		}

		private ContentRetriever<Post, int>.BrokenTokenWeightDeterminer GetStandardTokenWeightDeterminer()
		{
			return GetTokenWeightDeterminer(1f);
		}

		private ContentRetriever<Post, int>.BrokenTokenWeightDeterminer GetTokenWeightDeterminer(float multiplier)
		{
			if (multiplier <= 0)
				throw new ArgumentOutOfRangeException("multiplier", "must be greater than zero");
			return token => multiplier * (_stopWordLookup.Contains(token) ? 0.01f : 1f);
		}
	}
}
