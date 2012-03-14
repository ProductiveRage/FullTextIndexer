using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Common.Lists;
using Common.Logging;
using Common.StringComparisons;
using FullTextIndexer;
using FullTextIndexer.Indexes;
using FullTextIndexer.IndexGenerators;
using FullTextIndexer.TokenBreaking;
using Tester.KeyVariants;
using Tester.SourceData;

namespace Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            // TODO: Plurality handling??

            // Note: This data isn't yet included in the repository as what I'm using at the moment is from some private work set
            var data = (NonNullImmutableList<Product>)ReadFromDisk(new FileInfo("SampleData.dat"));

            var activeLanguageKeys = new[] { 1, 2 };

            var sourceStringComparer = new CaseInsensitiveAccentReplacingPunctuationRemovingWhitespaceStandardisingStringComparer();
            var contentRetrievers = new List<IndexGenerator<Product, IIndexKey>.ContentRetriever>();
            foreach (var languageKey in activeLanguageKeys)
            {
                // Take a copy of languageKey to ensure the correct value is used in the following PreBrokenContentRetriever lambdas
                var languageKeyForEntry = languageKey;
                contentRetrievers.Add(
                    new IndexGenerator<Product, IIndexKey>.ContentRetriever(
                        p => new IndexGenerator<Product, IIndexKey>.PreBrokenContent(
                            new LanguageScopedIndexKey(p.Key, languageKeyForEntry),
                            p.Name.GetTranslation(languageKeyForEntry)
                        ),
                        token => 15f * (Constants.StopWords.Contains(token, sourceStringComparer) ? 0.01f : 1f)
                    )
                );
                contentRetrievers.Add(
                    new IndexGenerator<Product, IIndexKey>.ContentRetriever(
                        p =>
                        {
                            if (p.Keywords == null)
                                return null;
                            return new IndexGenerator<Product, IIndexKey>.PreBrokenContent(
                                new LanguageScopedIndexKey(p.Key, languageKeyForEntry),
                                p.Keywords.GetTranslation(languageKeyForEntry)
                            );
                        },
                        token => 3f * (Constants.StopWords.Contains(token, sourceStringComparer) ? 0.01f : 1f)
                    )
                );
            }
            contentRetrievers.Add(GetNonScopedContentRetriever(p => p.Address == null ? null : p.Address.Address1, sourceStringComparer));
            contentRetrievers.Add(GetNonScopedContentRetriever(p => p.Address == null ? null : p.Address.Address2, sourceStringComparer));
            contentRetrievers.Add(GetNonScopedContentRetriever(p => p.Address == null ? null : p.Address.Address3, sourceStringComparer));
            contentRetrievers.Add(GetNonScopedContentRetriever(p => p.Address == null ? null : p.Address.Address4, sourceStringComparer));
            var indexGenerator = new IndexGenerator<Product, IIndexKey>(
                contentRetrievers.ToNonNullImmutableList(),
                new IndexKeyEqualityComparer(),
                sourceStringComparer,
                new WhiteSpaceTokenBreaker(
                    new CommaAndPeriodReplacingTokenBreaker(new NoActionTokenBreaker())
                ),
                weightedValues => weightedValues.Sum(),
                new ConsoleLogger()
            );
            var index = indexGenerator.Generate(data.ToNonNullImmutableList());

            var t1 = index.GetMatches("Test");
            var t2 = index.GetMatches("is");
            var t3 = index.GetMatches("it");
            //var t1 = index.GetMatches("This is a Test");
            //var t2 = index.GetMatches("This Test");
            //var t3 = index.GetMatches("Tèst");
            var t4 = index.GetMatches("Road");
            var t5 = index.GetMatches("is");
            var t6 = index.GetMatches("ceci");
            var t62 = FilterIndexKeyResults(t6, 1, 1);
            var t7 = index.GetMatches("Test Keywords");

            var t8 = index.GetMatches(
                "Test Keywords",
                new WhiteSpaceTokenBreaker(new CommaAndPeriodReplacingTokenBreaker(new NoActionTokenBreaker())),
                tokenMatches => tokenMatches.Sum(m => m.Weight / (5 * m.AllTokens.Count))
            );

            var t9 = t6.CombineResults(
                t8,
                new IndexKeyEqualityComparer(),
                matches => matches.Sum(m => m.Weight)
            );

            var indexWithoutProduct1 = index.Remove(k => k.ProductKey == 1);
            var indexWithoutProduct2 = index.Remove(k => k.ProductKey == 2);
        }

        private static NonNullImmutableList<WeightedEntry<int>> FilterIndexKeyResults(NonNullImmutableList<WeightedEntry<IIndexKey>> matches, int languageKey, int channelKey)
        {
            if (matches == null)
                throw new ArgumentNullException("matches");

            return matches
                .Where(m => m.Key.IsApplicableFor(languageKey, channelKey))
                .GroupBy(m => m.Key.ProductKey)
                .Select(g => new WeightedEntry<int>(g.Key, g.Sum(e => e.Weight)))
                .ToNonNullImmutableList();
        }

        private static IndexGenerator<Product, IIndexKey>.ContentRetriever GetNonScopedContentRetriever(Func<Product, string> valueRetriever, IEqualityComparer<string> sourceStringComparer)
        {
            if (valueRetriever == null)
                throw new ArgumentNullException("valueRetriever");
            if (sourceStringComparer == null)
                throw new ArgumentNullException("sourceStringComparer");

            return new IndexGenerator<Product, IIndexKey>.ContentRetriever(
                p =>
                {
                    var value = valueRetriever(p);
                    if (string.IsNullOrWhiteSpace(value))
                        return null;
                    return new IndexGenerator<Product, IIndexKey>.PreBrokenContent(
                        new NonScopedIndexKey(p.Key),
                        value
                    );
                },
                token => (Constants.StopWords.Contains(token, sourceStringComparer) ? 0.01f : 1f)
            );
        }

        private static TranslatedString GetTranslatedString(string defaultValue)
        {
            return new TranslatedString(defaultValue, new Dictionary<int, string>());
        }
        private static TranslatedString GetTranslatedString(string defaultValue, int languageKey0, string translation0)
        {
            return new TranslatedString(defaultValue, new Dictionary<int, string> { { languageKey0, translation0 } });
        }
        private static TranslatedString GetTranslatedString(string defaultValue, int languageKey0, string translation0, int languageKey1, string translation1)
        {
            return new TranslatedString(defaultValue, new Dictionary<int, string> { { languageKey0, translation0 }, { languageKey1, translation1 } });
        }

        public static object ReadFromDisk(FileInfo file)
        {
            if (file == null)
                throw new ArgumentNullException("file");

            using (var stream = File.Open(file.FullName, FileMode.Open))
            {
                return (new BinaryFormatter()).Deserialize(stream);
            }
        }
    }
}
