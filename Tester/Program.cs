using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Lists;
using FullTextIndexer.TokenBreaking;
using FullTextIndexer.IndexGenerators;

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
                StringComparer.InvariantCultureIgnoreCase,
                new ConsecutiveTokenCombiningTokenBreaker(
                    new AccentReplacingTokenBreaker(
                        new PunctuationRemovingTokenBreaker(
                            new WhiteSpaceTokenBreaker()
                        )
                    ),
                    5
                ),
                (token, occurenceCount) => occurenceCount // TODO: Downplay stopwords
            );

            var index = indexGenerator.Generate(data.ToNonNullImmutableList());

            var t1 = index.GetMatches("This is a Test");
            var t = index.GetMatches("This Test");
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
