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
using Tester.Example3.SourceData;

namespace Tester.Example3
{
    public static class Test
    {
        public static void Go()
        {
            var dataFile = new FileInfo("SampleData-Example3.dat");
            if (!dataFile.Exists)
                GenerateDataFile(dataFile, new FileInfo("NewYorkTimesAPIKey.txt"));

            var data = Serialisation.ReadFromDisk<NonNullImmutableList<Article>>(dataFile);
            var productIndexGenerator = new ProductIndexGenerator(
                new WhiteSpaceTokenBreaker(new CommaAndPeriodReplacingTokenBreaker(new NoActionTokenBreaker())),
                new CaseInsensitiveAccentReplacingPunctuationRemovingWhitespaceStandardisingStringComparer(),
                new ConsoleLogger()
            );
            var index = productIndexGenerator.Generate(data);

            var matchesOverSingleField = GetMatches(
                index,
                "Exercise",
                new WhiteSpaceTokenBreaker(new CommaAndPeriodReplacingTokenBreaker(new NoActionTokenBreaker()))
            );

            var matchesOverMultipleFields = GetMatches(
                index,
                "Penguins Steamrolling Christopher",
                new WhiteSpaceTokenBreaker(new CommaAndPeriodReplacingTokenBreaker(new NoActionTokenBreaker()))
            );
        }

        private static NonNullImmutableList<WeightedEntry<int>> GetMatches(IndexData<int> index, string source, ITokenBreaker tokenBreaker)
        {
            if (string.IsNullOrWhiteSpace(source))
                throw new ArgumentException("Null/empty source");
            if (tokenBreaker == null)
                throw new ArgumentNullException("tokenBreaker");

            // Build up sets of matches for each token, matches are indexed by Product Key
            var matchSets = new List<NonNullImmutableList<WeightedEntry<int>>>();
            foreach (var token in tokenBreaker.Break(source).Distinct(index.TokenComparer))
                matchSets.Add(index.GetMatches(token));

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

        private static void GenerateDataFile(FileInfo dataFile, FileInfo apiDataFile)
        {
            if (dataFile == null)
                throw new ArgumentNullException("dataFile");
            if (apiDataFile == null)
                throw new ArgumentNullException("apiDataFile");
            if (!apiDataFile.Exists)
                throw new ArgumentException("The apiDataFile does not exist (you'll have to register with NYT to get a key and then store it in " + apiDataFile.FullName);

            // Note: I haven't added in the API Key file - you'll have to register with NYT and generate one yourself (see http://developer.nytimes.com/)
            var apiKey = File.ReadAllText(apiDataFile.FullName);
            Serialisation.WriteToDisk(
                dataFile,
                new ArticlesDataLoader(
                    new NewYorkTimesArticleRetriever(apiKey),
                    new ConsoleLogger()
                ).GetArticles("penguins", int.MaxValue)
            );
        }
    }
}
