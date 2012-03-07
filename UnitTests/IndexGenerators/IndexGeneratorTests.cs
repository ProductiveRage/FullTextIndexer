﻿using System;
using System.Collections.Generic;
using System.Linq;
using Common.Lists;
using FullTextIndexer.Indexes;
using FullTextIndexer.IndexGenerators;
using FullTextIndexer.TokenBreaking;
using Xunit;

namespace UnitTests.IndexGenerators
{
    public class IndexGeneratorTests
    {
        [Fact]
        public void SingleProductWithSingleWordName()
        {
            var indexGenerator = new IndexGenerator<Product, int>(
                new NonNullImmutableList<IndexGenerator<Product,int>.ContentRetriever>(new[]
                {
                    new IndexGenerator<Product,int>.ContentRetriever(
                        p => new IndexGenerator<Product, int>.PreBrokenContent(p.Key, p.Name),
                        token => 1f
                    )                        
                }),
                new IntEqualityComparer(),
                StringComparer.InvariantCultureIgnoreCase,
                new WhiteSpaceTokenBreaker(new NoActionTokenBreaker()),
                weightedValues => weightedValues.Sum()
            );
            var index = indexGenerator.Generate(new NonNullImmutableList<Product>(new[]
            {
                new Product() { Key = 1, Name = "Product" }
            }));

            var expected = new NonNullImmutableList<WeightedEntry<int>>(new[]
            {
                new WeightedEntry<int>(1, 1f)
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
                var keyComparison = x.Key.CompareTo(y);
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
