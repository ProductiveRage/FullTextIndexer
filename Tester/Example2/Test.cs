using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common.Lists;
using Common.Logging;
using Common.StringComparisons;
using FullTextIndexer.Indexes;
using FullTextIndexer.TokenBreaking;
using Tester.Common;
using Tester.Example2.KeyVariants;
using Tester.Example2.SourceData;

namespace Tester.Example2
{
    public static class Test
    {
        public static void Go()
        {
            // TODO: Plurality handling??

            // Note: This data isn't yet included in the repository as what I'm using at the moment is from some private work set
            var dataFile = new FileInfo("SampleData-Example2.dat");
            /*
            Serialisation.WriteToDisk(
                dataFile,
                new PubDataLoader("server=.\\SQLExpress;database=Pub;Trusted_Connection=True;").GetProducts()
            );
             */

            var english = new LanguageDetails(1, "en");
            var data = Serialisation.ReadFromDisk<NonNullImmutableList<Product>>(dataFile);
            var productIndexGenerator = new ProductIndexGenerator(
                new NonNullImmutableList<LanguageDetails>(new [] { english }),
                english,
                new WhiteSpaceTokenBreaker(new CommaAndPeriodReplacingTokenBreaker(new NoActionTokenBreaker())),
                new CaseInsensitiveAccentReplacingPunctuationRemovingWhitespaceStandardisingStringComparer(),
                new ConsoleLogger()
            );
            var index = productIndexGenerator.Generate(data);

            var matchesOverSingleField = GetMatches(
                index,
                "Exercise",
                new WhiteSpaceTokenBreaker(new CommaAndPeriodReplacingTokenBreaker(new NoActionTokenBreaker())),
                english,
                1
            );

            var matchesOverMultipleFields = GetMatches(
                index,
                "Fear Moon Exercise, Boston",
                new WhiteSpaceTokenBreaker(new CommaAndPeriodReplacingTokenBreaker(new NoActionTokenBreaker())),
                english,
                1
            );
        }

        private static NonNullImmutableList<WeightedEntry<int>> GetMatches(IndexData<IIndexKey> index, string source, ITokenBreaker tokenBreaker, LanguageDetails language, int channelKey)
        {
            if (string.IsNullOrWhiteSpace(source))
                throw new ArgumentException("Null/empty source");
            if (tokenBreaker == null)
                throw new ArgumentNullException("tokenBreaker");
            if (language == null)
                throw new ArgumentNullException("language");

            // Build up sets of matches for each token, matches are filtered to the specified Language and Channel and indexed by Product Key
            var matchSets = new List<NonNullImmutableList<WeightedEntry<int>>>();
            foreach (var token in tokenBreaker.Break(source).Distinct(index.TokenComparer))
                matchSets.Add(FilterIndexKeyResults(index.GetMatches(token), language, channelKey));

            // Construct a list of Product Keys that exist in all of the match sets
            var keysInAllSets = matchSets.SelectMany(s => s.Select(m => m.Key)).Distinct();
            foreach (var matchSet in matchSets)
                keysInAllSets = keysInAllSets.Intersect(matchSet.Select(m => m.Key));

            // Map this back to the matches for keys where the keys exist in all of the match sets
            var combinedResults = new Dictionary<int, List<float>>();
            foreach (var matchSet in matchSets)
            {
                foreach (var match in matchSet.Where(m => keysInAllSets.Contains(m.Key)))
                {
                    if (!combinedResults.ContainsKey(match.Key))
                        combinedResults.Add(match.Key, new List<float>());
                    combinedResults[match.Key].Add(match.Weight);
                }
            }
            return combinedResults.Select(entry => new WeightedEntry<int>(entry.Key, entry.Value.Sum())).ToNonNullImmutableList();
        }

        /// <summary>
        /// Given a a set of matches indexed on IIndexKey, filter based upon the specified language and channel and return matches based indexed on the Product Key, combining any
        /// repeated Product Keys by summing the match weights
        /// </summary>
        private static NonNullImmutableList<WeightedEntry<int>> FilterIndexKeyResults(NonNullImmutableList<WeightedEntry<IIndexKey>> matches, LanguageDetails language, int channelKey)
        {
            if (matches == null)
                throw new ArgumentNullException("matches");
            if (language == null)
                throw new ArgumentNullException("language");

            return matches
                .Where(m => m.Key.IsApplicableFor(language, channelKey))
                .GroupBy(m => m.Key.ProductKey)
                .Select(g => new WeightedEntry<int>(g.Key, g.Sum(e => e.Weight)))
                .ToNonNullImmutableList();
        }
    }
}
