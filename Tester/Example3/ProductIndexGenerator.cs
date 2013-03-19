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
using Tester.Example3.SourceData;

namespace Tester.Example3
{
    public class ProductIndexGenerator
    {
        private ITokenBreaker _tokenBreaker;
        private IStringNormaliser _sourceStringComparer;
        private ILogger _logger;
        public ProductIndexGenerator(ITokenBreaker tokenBreaker, IStringNormaliser sourceStringComparer, ILogger logger)
        {
            if (tokenBreaker == null)
                throw new ArgumentNullException("tokenBreaker");
            if (sourceStringComparer == null)
                throw new ArgumentNullException("sourceStringComparer");
            if (logger == null)
                throw new ArgumentNullException("logger");

            _tokenBreaker = tokenBreaker;
            _sourceStringComparer = sourceStringComparer;
            _logger = logger;
        }

        public IIndexData<int> Generate(NonNullImmutableList<Article> data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            var contentRetrievers = new List<ContentRetriever<Article, int>>();
            contentRetrievers.Add(new ContentRetriever<Article, int>(
				a => new PreBrokenContent<int>(a.Key, a.Title),
                GetTokenWeightDeterminer(15f)
            ));
            contentRetrievers.Add(new ContentRetriever<Article, int>(
				a => new PreBrokenContent<int>(a.Key, a.ByLine),
                GetTokenWeightDeterminer(3f)
			));
            contentRetrievers.Add(new ContentRetriever<Article, int>(
				a => new PreBrokenContent<int>(a.Key, a.Keywords),
                GetTokenWeightDeterminer(3f)
			));
            contentRetrievers.Add(new ContentRetriever<Article, int>(
				a => new PreBrokenContent<int>(a.Key, a.Body),
                GetTokenWeightDeterminer(1f)
			));
                
            return new IndexGenerator<Article, int>(
                contentRetrievers.ToNonNullImmutableList(),
                new DefaultEqualityComparer<int>(),
                _sourceStringComparer,
                _tokenBreaker,
                weightedValues => weightedValues.Sum(),
                _logger
            ).Generate(data.ToNonNullImmutableList());
        }

        private ContentRetriever<Article, int>.BrokenTokenWeightDeterminer GetTokenWeightDeterminer(float multiplier)
        {
            if (multiplier <= 0)
                throw new ArgumentOutOfRangeException("multiplier", "must be greater than zero");
            return token => multiplier * (Constants.GetStopWords("en").Contains(token, _sourceStringComparer) ? 0.01f : 1f);
        }
    }
}
