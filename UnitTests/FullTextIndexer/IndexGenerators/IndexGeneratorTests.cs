using System;
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
				NonNullImmutableList.Create(
                    new ContentRetriever<Product, int>(
                        p => new PreBrokenContent<int>(p.Key, p.Name),
                        token => 1f
                    )                        
                ),
				new DefaultEqualityComparer<int>(),
				new CaseInsensitiveStringNormaliser(),
				new WhiteSpaceTokenBreaker(),
				weightedValues => weightedValues.Sum(),
				captureSourceLocations: true,
				logger: new NullLogger()
			);
			var index = indexGenerator.Generate(NonNullImmutableList.Create(
                new Product() { Key = 1, Name = "Product" }
            ));

			var expected = NonNullImmutableList.Create(
                new WeightedEntry<int>(1, 1f, (new[] { new SourceFieldLocation(0, 0, 0, 7, 1f) }).ToNonNullImmutableList())
            );
			EnsureIndexDataMatchesExpectations(
				expected,
				index.GetMatches("Product")
			);
		}

		[Fact]
		public void SingleProductWithSingleWordNameAndSameSingleWordDescription()
		{
			var indexGenerator = new IndexGenerator<ProductWithDescription, int>(
				NonNullImmutableList.Create(
                    new ContentRetriever<ProductWithDescription, int>(
                        p => new PreBrokenContent<int>(p.Key, p.Name),
                        token => 1f
                    ),
                    new ContentRetriever<ProductWithDescription, int>(
                        p => new PreBrokenContent<int>(p.Key, p.Description),
                        token => 1f
                    )                        
                ),
				new DefaultEqualityComparer<int>(),
				new CaseInsensitiveStringNormaliser(),
				new WhiteSpaceTokenBreaker(),
				weightedValues => weightedValues.Sum(),
				captureSourceLocations: true,
				logger: new NullLogger()
			);
			var index = indexGenerator.Generate(NonNullImmutableList.Create(
                new ProductWithDescription() { Key = 1, Name = "Product", Description = "Product" }
            ));

			var expected = NonNullImmutableList.Create(
                new WeightedEntry<int>(
					1,
					2f,
					(new[]
					{
						new SourceFieldLocation(0, 0, 0, 7, 1f), // Match in Name field (source field index 0)
						new SourceFieldLocation(1, 0, 0, 7, 1f)  // Match in Description field (source field index 1)
					}).ToNonNullImmutableList())
            );
			EnsureIndexDataMatchesExpectations(
				expected,
				index.GetMatches("Product")
			);
		}

		/// <summary>
		/// Only the first content retriever may result in SourceFieldLocation instances with a SourceFieledIndex of zero, regardless of whether or not it returns
		/// any content. This is important for the enabling of search term highlighting.
		/// </summary>
		[Fact]
		public void IfTheFirstContentRetrieverReturnsNoContentThenNoSourceFieldIndexZeroSourceLocationsAreReturned()
		{
			var indexGenerator = new IndexGenerator<ProductWithDescription, int>(
				NonNullImmutableList.Create(
                    new ContentRetriever<ProductWithDescription, int>(
                        p => new PreBrokenContent<int>(p.Key, p.Name),
                        token => 1f
                    ),
                    new ContentRetriever<ProductWithDescription, int>(
                        p => new PreBrokenContent<int>(p.Key, p.Description),
                        token => 1f
                    )                        
                ),
				new DefaultEqualityComparer<int>(),
				new CaseInsensitiveStringNormaliser(),
				new WhiteSpaceTokenBreaker(),
				weightedValues => weightedValues.Sum(),
				captureSourceLocations: true,
				logger: new NullLogger()
			);
			var index = indexGenerator.Generate(NonNullImmutableList.Create(
                new ProductWithDescription() { Key = 1, Name = "", Description = "Product" }
            ));

			var expected = NonNullImmutableList.Create(
                new WeightedEntry<int>(
					1,
					1f,
					NonNullImmutableList.Create(new SourceFieldLocation(1, 0, 0, 7, 1f))  // Match in Description field (source field index 1)
				)
            );
			EnsureIndexDataMatchesExpectations(
				expected,
				index.GetMatches("Product")
			);
		}

		[Fact]
		public void TestRemovalOfResultFromIndex()
		{
			var indexGenerator = new IndexGenerator<ProductWithDescription, int>(
				NonNullImmutableList.Create(
					new ContentRetriever<ProductWithDescription, int>(
						p => new PreBrokenContent<int>(p.Key, p.Name),
						token => 1f
					),
					new ContentRetriever<ProductWithDescription, int>(
						p => new PreBrokenContent<int>(p.Key, p.Description),
						token => 1f
					)
				),
				new DefaultEqualityComparer<int>(),
				new CaseInsensitiveStringNormaliser(),
				new WhiteSpaceTokenBreaker(),
				weightedValues => weightedValues.Sum(),
				captureSourceLocations: true,
				logger: new NullLogger()
			);
			var index = indexGenerator.Generate(NonNullImmutableList.Create(
				new ProductWithDescription() { Key = 1, Name = "", Description = "Product" },
				new ProductWithDescription() { Key = 2, Name = "", Description = "Product" }
			));
			Assert.Equal(2, index.GetMatches("Product").Count); // Should get two matches for "Product" at this point
			Assert.Equal(1, index.Remove(key => key == 2).GetMatches("Product").Count); // Should get only one if remove results for Key 2
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

            expected = expected.Sort(Sort);
            actual = actual.Sort(Sort);
            
            for (var index = 0; index < expected.Count; index++)
            {
                if ((expected[index].Key != actual[index].Key) || (expected[index].Weight != actual[index].Weight))
                    throw new ArgumentException("expected's content do not match actual's");
            }

			int Sort(WeightedEntry<int> x, WeightedEntry<int> y)
			{
				var keyComparison = x.Key.CompareTo(y.Key);
				if (keyComparison != 0)
					return keyComparison;
				return x.Weight.CompareTo(y.Weight);
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
