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
using Tester.Example1.SourceData;

namespace Tester.Example1
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

        public IIndexData<int> Generate(NonNullImmutableList<Product> data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            var contentRetrievers = new List<ContentRetriever<Product, int>>();

            // Instantiate content retriever for the Name and Keywords, these have higher weights assigned to them than the other fields
            contentRetrievers.Add(new ContentRetriever<Product, int>(
				p => new PreBrokenContent<int>(p.Key, p.Name),
				GetTokenWeightDeterminer(15f)
            ));
            contentRetrievers.Add(new ContentRetriever<Product, int>(
				p => new PreBrokenContent<int>(p.Key, p.Keywords),
				GetTokenWeightDeterminer(3f)
            ));

            // Instantiate content retriever for the Description field (ignore any null Description values)
            contentRetrievers.Add(new ContentRetriever<Product, int>(
                p => new PreBrokenContent<int>(p.Key, p.Description),
                GetStandardTokenWeightDeterminer()
            ));

            // Instantiate content retriever for the Address fields (ignore any null Address values or null fields within an Address)
            contentRetrievers.Add(new ContentRetriever<Product, int>(
                GetRetrieverForAddress(),
                GetStandardTokenWeightDeterminer()
            ));
                
            return new IndexGenerator<Product, int>(
                contentRetrievers.ToNonNullImmutableList(),
                new DefaultEqualityComparer<int>(),
                _sourceStringComparer,
                _tokenBreaker,
                weightedValues => weightedValues.Sum(),
                _logger
            ).Generate(data.ToNonNullImmutableList());
        }

        private ContentRetriever<Product, int>.PreBrokenTokenContentRetriever GetRetrieverForAddress()
        {
            return p =>
            {
				var content = new NonNullOrEmptyStringList();
				if (p.Address != null)
				{
					foreach (var addressSection in new[] { p.Address.Address1, p.Address.Address2, p.Address.Address3, p.Address.Address4, p.Address.Address5, p.Address.Country})
					{
						if (!string.IsNullOrWhiteSpace(addressSection))
							content = content.Add(addressSection);
					}
				}
                return new PreBrokenContent<int>(p.Key, content);
            };
        }

        private ContentRetriever<Product, int>.BrokenTokenWeightDeterminer GetStandardTokenWeightDeterminer()
        {
            return GetTokenWeightDeterminer(1f);
        }

        private ContentRetriever<Product, int>.BrokenTokenWeightDeterminer GetTokenWeightDeterminer(float multiplier)
        {
            if (multiplier <= 0)
                throw new ArgumentOutOfRangeException("multiplier", "must be greater than zero");
            return token => multiplier * (Constants.GetStopWords("en").Contains(token, _sourceStringComparer) ? 0.01f : 1f);
        }
    }
}
