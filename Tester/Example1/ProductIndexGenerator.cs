using System;
using System.Collections.Generic;
using System.Linq;
using Common.Lists;
using Common.Logging;
using FullTextIndexer;
using FullTextIndexer.Indexes;
using FullTextIndexer.IndexGenerators;
using FullTextIndexer.TokenBreaking;
using Tester.Example1.SourceData;

namespace Tester.Example1
{
    public class ProductIndexGenerator
    {
        private ITokenBreaker _tokenBreaker;
        private IEqualityComparer<string> _sourceStringComparer;
        private ILogger _logger;
        public ProductIndexGenerator(ITokenBreaker tokenBreaker, IEqualityComparer<string> sourceStringComparer, ILogger logger)
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

        public IndexData<int> Generate(NonNullImmutableList<Product> data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            var stopwords = Constants.GetStopWords("en");
            var contentRetrievers = new List<ContentRetriever<Product, int>>();

            // Instantiate content retriever for the Name and Keywords, these have higher weights assigned to them than the other fields (ignore any null Keywords values)
            contentRetrievers.Add(new ContentRetriever<Product, int>(
                GetRetrieverForProductName(),
                GetTokenWeightDeterminer(15f)
            ));
            contentRetrievers.Add(new ContentRetriever<Product, int>(
                GetRetrieverForProductKeywords(),
                GetTokenWeightDeterminer(3f)
            ));

            // Instantiate content retriever for the Description field (ignore any null Description values)
            contentRetrievers.Add(new ContentRetriever<Product, int>(
                GetRetrieverForDescription(),
                GetStandardTokenWeightDeterminer()
            ));

            // Instantiate content retriever for the Address fields (ignore any null Address values or null fields within an Address)
            contentRetrievers.Add(new ContentRetriever<Product, int>(
                GetRetrieverForAddress(a => a.Address1),
                GetStandardTokenWeightDeterminer()
            ));
            contentRetrievers.Add(new ContentRetriever<Product, int>(
                GetRetrieverForAddress(a => a.Address2),
                GetStandardTokenWeightDeterminer()
            ));
            contentRetrievers.Add(new ContentRetriever<Product, int>(
                GetRetrieverForAddress(a => a.Address3),
                GetStandardTokenWeightDeterminer()
            ));
            contentRetrievers.Add(new ContentRetriever<Product, int>(
                GetRetrieverForAddress(a => a.Address4),
                GetStandardTokenWeightDeterminer()
            ));
            contentRetrievers.Add(new ContentRetriever<Product, int>(
                GetRetrieverForAddress(a => a.Address5),
                GetStandardTokenWeightDeterminer()
            ));
            contentRetrievers.Add(new ContentRetriever<Product, int>(
                GetRetrieverForAddress(a => a.Country),
                GetStandardTokenWeightDeterminer()
            )); 
                
            return new IndexGenerator<Product, int>(
                contentRetrievers.ToNonNullImmutableList(),
                new IntEqualityComparer(),
                _sourceStringComparer,
                _tokenBreaker,
                weightedValues => weightedValues.Sum(),
                _logger
            ).Generate(data.ToNonNullImmutableList());
        }

        private ContentRetriever<Product, int>.PreBrokenTokenContentRetriever GetRetrieverForProductName()
        {
            return p => new PreBrokenContent<int>(p.Key, p.Name);
        }

        private ContentRetriever<Product, int>.PreBrokenTokenContentRetriever GetRetrieverForProductKeywords()
        {
            return p => p.Keywords == "" ? null : new PreBrokenContent<int>(p.Key, p.Keywords);
        }

        private ContentRetriever<Product, int>.PreBrokenTokenContentRetriever GetRetrieverForDescription()
        {
            return p => p.Description == "" ? null : new PreBrokenContent<int>(p.Key, p.Description);
        }

        private ContentRetriever<Product, int>.PreBrokenTokenContentRetriever GetRetrieverForAddress(Func<AddressDetails, string> addressValueRetriever)
        {
            if (addressValueRetriever == null)
                throw new ArgumentNullException("addressValueRetriever");
            return p =>
            {
                if (p.Address == null)
                    return null;
                var value = addressValueRetriever(p.Address);
                if (string.IsNullOrWhiteSpace(value))
                    return null;
                return new PreBrokenContent<int>(p.Key, value);
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
        
        private class IntEqualityComparer : IEqualityComparer<int>
        {
            public bool Equals(int x, int y)
            {
                return (x == y);
            }
            public int GetHashCode(int obj)
            {
                return obj;
            }
        }
    }
}
