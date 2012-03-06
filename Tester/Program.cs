using System;
using System.Collections.Generic;
using System.Linq;
using Common.Lists;
using Common.StringComparisons;
using FullTextIndexer;
using FullTextIndexer.Indexes;
using FullTextIndexer.IndexGenerators;
using FullTextIndexer.TokenBreaking;

namespace Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            // TODO: Partial match handling (with appropriate weight adjustment)
            // TODO: Plurality handling??
            var data = new[]
            {
                new Product(1, "This is a tést", "keywords key1 it"),
                new Product(2, "This is also a tést, yes it is", "keywords key2 it")
            }.ToNonNullImmutableList();

            var sourceStringComparer = new CaseInsensitiveAccentReplacingPunctuationRemovingWhitespaceStandardisingStringComparer();
            var indexGenerator = new IndexGenerator<Product, int>(
                new NonNullImmutableList<IndexGenerator<Product,int>.ContentRetriever>(new[]
                {
                    new IndexGenerator<Product,int>.ContentRetriever(
                        p => new KeyValuePair<int, string>(p.Key, p.Name),
                        token => 1f * (Constants.StopWords.Contains(token, sourceStringComparer) ? 0.01f : 1f)
                    ),
                    new IndexGenerator<Product,int>.ContentRetriever(
                        p => new KeyValuePair<int, string>(p.Key, p.Keywords),
                        token => 3f * (Constants.StopWords.Contains(token, sourceStringComparer) ? 0.01f : 1f)
                    )
                }),
                new IntEqualityComparer(),
                sourceStringComparer,
                new ConsecutiveTokenCombiningTokenBreaker(
                    new WhiteSpaceTokenBreaker(
                        new CommaAndPeriodReplacingTokenBreaker(new NoActionTokenBreaker())
                    ),
                    5
                ),
                weightedValues => weightedValues.Sum()
            );
            var index = indexGenerator.Generate(data);

            var t1 = index.GetMatches("Test");
            var t2 = index.GetMatches("is");
            var t3 = index.GetMatches("it");
            //var t1 = index.GetMatches("This is a Test");
            //var t2 = index.GetMatches("This Test");
            //var t3 = index.GetMatches("Tèst");
            //var t4 = index.GetMatches("a");
            //var t5 = index.GetMatches("is");
            var t6 = index.GetMatches("it");
            var t7 = index.GetMatches("Test Keywords");

            var t8 = index.GetMatches(
                "Test Keywords",
                new WhiteSpaceTokenBreaker(new CommaAndPeriodReplacingTokenBreaker(new NoActionTokenBreaker())),
                tokenMatches => tokenMatches.Sum(m => m.Weight / (5 * m.AllTokens.Count))
            );

            var t9 = t6.CombineResults(
                t8,
                new IntEqualityComparer(),
                matches => matches.Sum(m => m.Weight)
            );

            var indexWithoutProduct1 = index.RemoveEntriesFor((new[] { 1 }).ToImmutableList());
            var indexWithoutProduct2 = index.RemoveEntriesFor((new[] { 2 }).ToImmutableList());
        }

        public class Product
        {
            public Product(int key, string name, string keywords)
            {
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("Null/blank name specified");

                Key = key;
                Name = name;
                Keywords = string.IsNullOrWhiteSpace(keywords) ? "" : keywords.Trim();
            }

            public int Key { get; private set; }

            /// <summary>
            /// This will never be null or empty
            /// </summary>
            public string Name { get; private set; }

            /// <summary>
            /// This will never be null but it may be empty
            /// </summary>
            public string Keywords { get; private set; }
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
