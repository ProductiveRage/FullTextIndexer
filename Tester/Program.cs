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
                new Product(1, "This is a tést"),
                new Product(2, "This is also a tést, yes it is"),
            };

            var indexGenerator = new IndexGenerator<Product, int>(
                p => p.Key,
                new IntEqualityComparer(),
                p => p.Name,
                new CaseInsensitiveAccentReplacingPunctuationRemovingStringComparer(),
                new ConsecutiveTokenCombiningTokenBreaker(
                    new WhiteSpaceTokenBreaker(
                        new CommaAndPeriodReplacingTokenBreaker(new NoActionTokenBreaker())
                    ),
                    5
                ),
                (token, occurenceCount) => occurenceCount * (Constants.StopWords.Contains(token, StringComparer.InvariantCultureIgnoreCase) ? 0.01f : 1)
            );

            var index = indexGenerator.Generate(data.ToNonNullImmutableList());

            //var t1 = index.GetMatches("This is a Test");
            //var t2 = index.GetMatches("This Test");
            var t3 = index.GetMatches("Test");
            var t4 = index.GetMatches("a");
            var t5 = index.GetMatches("is");
        }

        public class Product
        {
            public Product(int key, string name)
            {
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("Null/blank name specified");

                Key = key;
                Name = name;
            }

            public int Key { get; private set; }
            
            /// <summary>
            /// This will never be null or empty
            /// </summary>
            public string Name { get; private set; }
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
