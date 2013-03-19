using System;
using System.Collections.Generic;
using System.Linq;
using FullTextIndexer.Common.Lists;
using FullTextIndexer.Common.Logging;
using FullTextIndexer.Core.Indexes;
using FullTextIndexer.Core.Indexes.TernarySearchTree;
using FullTextIndexer.Core.IndexGenerators;
using FullTextIndexer.Core.TokenBreaking;
using Xunit;

namespace UnitTests.FullTextIndexer.IndexGenerators
{
    public class IndexGeneratorTests
    {
		[Fact]
		public void SingleProductWithSingleWordName()
		{
			var indexGenerator = new IndexGenerator<Product, int>(
				new NonNullImmutableList<ContentRetriever<Product, int>>(new[]
                {
                    new ContentRetriever<Product, int>(
                        p => new PreBrokenContent<int>(p.Key, p.Name),
                        token => 1f
                    )                        
                }),
				new DefaultEqualityComparer<int>(),
				new CaseInsensitiveStringNormaliser(),
				new WhiteSpaceTokenBreaker(),
				weightedValues => weightedValues.Sum(),
				new NullLogger()
			);
			var index = indexGenerator.Generate(new NonNullImmutableList<Product>(new[]
            {
                new Product() { Key = 1, Name = "Product" }
            }));

			var expected = new NonNullImmutableList<WeightedEntry<int>>(new[]
            {
                new WeightedEntry<int>(1, 1f, (new[] { new SourceFieldLocation(0, 0, 0, 7) }).ToNonNullImmutableList())
            });
			EnsureIndexDataMatchesExpectations(
				expected,
				index.GetMatches("Product")
			);
		}

		[Fact]
		public void SingleProductWithSingleWordNameAndSameSingleWordDescription()
		{
			var indexGenerator = new IndexGenerator<ProductWithDescription, int>(
				new NonNullImmutableList<ContentRetriever<ProductWithDescription, int>>(new[]
                {
                    new ContentRetriever<ProductWithDescription, int>(
                        p => new PreBrokenContent<int>(p.Key, p.Name),
                        token => 1f
                    ),
                    new ContentRetriever<ProductWithDescription, int>(
                        p => new PreBrokenContent<int>(p.Key, p.Description),
                        token => 1f
                    )                        
                }),
				new DefaultEqualityComparer<int>(),
				new CaseInsensitiveStringNormaliser(),
				new WhiteSpaceTokenBreaker(),
				weightedValues => weightedValues.Sum(),
				new NullLogger()
			);
			var index = indexGenerator.Generate(new NonNullImmutableList<ProductWithDescription>(new[]
            {
                new ProductWithDescription() { Key = 1, Name = "Product", Description = "Product" }
            }));

			var expected = new NonNullImmutableList<WeightedEntry<int>>(new[]
            {
                new WeightedEntry<int>(
					1,
					2f,
					(new[]
					{
						new SourceFieldLocation(0, 0, 0, 7), // Match in Name field (source field index 0)
						new SourceFieldLocation(1, 0, 0, 7)  // Match in Description field (source field index 1)
					}).ToNonNullImmutableList())
            });
			EnsureIndexDataMatchesExpectations(
				expected,
				index.GetMatches("Product")
			);
		}

		/// <summary>
        /// This will throw an exception if the contents of expected do not match that of actual (or if either reference is null)
        /// </summary>
        private void EnsureIndexDataMatchesExpectations(NonNullImmutableList<WeightedEntry<int>> expected, NonNullImmutableList<WeightedEntry<int>> actual)
        {
            if (expected == null)
                throw new ArgumentNullException("expected");
            if (actual == null)
                throw new ArgumentNullException("actual");

            if (expected.Count != actual.Count)
                throw new ArgumentException("expected.Count does not match actual.Count");

            Comparison<WeightedEntry<int>> sorter = (x, y) =>
            {
                var keyComparison = x.Key.CompareTo(y.Key);
                if (keyComparison != 0)
                    return keyComparison;
                return x.Weight.CompareTo(y.Weight);
            };
            expected = expected.Sort(sorter);
            actual = actual.Sort(sorter);
            
            for (var index = 0; index < expected.Count; index++)
            {
                if ((expected[index].Key != actual[index].Key) || (expected[index].Weight != actual[index].Weight))
                    throw new ArgumentException("expected's content do not match actual's");
            }
        }

		private class Product
		{
			public int Key { get; set; }
			public string Name { get; set; }
		}

        private class ProductWithDescription : Product
        {
			public string Description { get; set; }
		}

        private class CaseInsensitiveStringNormaliser : IStringNormaliser
        {
            public string GetNormalisedString(string value)
            {
                return value.ToLower();
            }

            public bool Equals(string x, string y)
            {
                return GetNormalisedString(x) == GetNormalisedString(y);
            }

            public int GetHashCode(string obj)
            {
                return GetNormalisedString(obj).GetHashCode();
            }
        }
    }
}
