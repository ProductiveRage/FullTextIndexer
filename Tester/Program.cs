using System;
using System.Collections.Generic;
using Common.Lists;
using Common.StringComparisons;
using FullTextIndexer;
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
            };

            var sourceStringComparer = new CaseInsensitiveAccentReplacingPunctuationRemovingWhitespaceStandardisingStringComparer();
            var dataKeyComparer = new IntEqualityComparer();
            var indexGenerator = new AdditionBasedCombiningIndexGenerator<Product, int>(
                new NonNullImmutableList<IIndexGenerator<Product, int>>(new[]
                {
                    GetIndexGeneratorForProperty(p => p.Name, sourceStringComparer, dataKeyComparer, 1f),
                    GetIndexGeneratorForProperty(p => p.Keywords, sourceStringComparer, dataKeyComparer, 1f)
                }),
                sourceStringComparer,
                dataKeyComparer
            );
            var index = indexGenerator.Generate(data.ToNonNullImmutableList());

            //var t1 = index.GetMatches("This is a Test");
            //var t2 = index.GetMatches("This Test");
            //var t3 = index.GetMatches("Tèst");
            //var t4 = index.GetMatches("a");
            //var t5 = index.GetMatches("is");
            var t6 = index.GetMatches("it");
        }

        private static IIndexGenerator<Product, int> GetIndexGeneratorForProperty(
            IndexGenerator<Product, int>.SourceRetriever propertyRetriever,
            IEqualityComparer<string> sourceStringComparer,
            IEqualityComparer<int> dataKeyComparer,
            float weightMultiplier)
        {
            if (propertyRetriever == null)
                throw new ArgumentNullException("propertyRetriever");
            if (weightMultiplier <= 0)
                throw new ArgumentOutOfRangeException("weightMultiplier", "must be > 0");

            return new IndexGenerator<Product, int>(
                p => p.Key,
                dataKeyComparer,
                propertyRetriever,
                sourceStringComparer,
                new ConsecutiveTokenCombiningTokenBreaker(
                    new WhiteSpaceTokenBreaker(
                        new CommaAndPeriodReplacingTokenBreaker(new NoActionTokenBreaker())
                    ),
                    5
                ),
                (token, occurenceCount) =>
                    occurenceCount
                    * weightMultiplier
                    * (Constants.StopWords.Contains(token, sourceStringComparer) ? 0.01f : 1)
            );
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
