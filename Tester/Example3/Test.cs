using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common.Lists;
using Common.Logging;
using FullTextIndexer.Indexes;
using FullTextIndexer.Indexes.TernarySearchTree;
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
                new WhiteSpaceExtendingTokenBreaker(
                    new ImmutableList<char>(new[] { '<', '>', '[', ']', '(', ')', '{', '}', '.', ',' }),
                    new WhiteSpaceTokenBreaker()
                ),
                new EnglishPluralityStringNormaliser(
                    new DefaultStringNormaliser(),
                    EnglishPluralityStringNormaliser.PreNormaliserWorkOptions.PreNormaliserLowerCases | EnglishPluralityStringNormaliser.PreNormaliserWorkOptions.PreNormaliserTrims
                ),
                new ConsoleLogger()
            );
            var index = productIndexGenerator.Generate(data);

            var matchesOverSingleField = GetMatches(
                index,
                "Exercise",
                new WhiteSpaceExtendingTokenBreaker(
                    new ImmutableList<char>(new[] { '<', '>', '[', ']', '(', ')', '{', '}', '.', ',' }),
                    new WhiteSpaceTokenBreaker()
                )
            );

            var matchesOverMultipleFields = GetMatches(
                index,
                "Penguins Slap Christopher",
                new WhiteSpaceExtendingTokenBreaker(
                    new ImmutableList<char>(new[] { '<', '>', '[', ']', '(', ')', '{', '}', '.', ',' }),
                    new WhiteSpaceTokenBreaker()
                )
            );
        }

        /// <summary>
        /// Find results that have all of the tokens in the specified source string somewhere in their data (not necessarily in the same fields)
        /// </summary>
        private static NonNullImmutableList<WeightedEntry<int>> GetMatches(IIndexData<int> index, string source, ITokenBreaker tokenBreaker)
        {
            if (string.IsNullOrWhiteSpace(source))
                throw new ArgumentException("Null/empty source");
            if (tokenBreaker == null)
                throw new ArgumentNullException("tokenBreaker");

            return index.GetPartialMatches(
                source,
                tokenBreaker,
                (tokenMatches, allTokens) => (tokenMatches.Count < allTokens.Count) ? 0 : tokenMatches.SelectMany(m => m.Weights).Sum()
            );
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
                    new NewYorkTimesArticleRetriever(new Uri("http://api.nytimes.com/svc/search/v1/article"), apiKey),
                    5, // maxConsecutiveFailCount
                    new ConsoleLogger()
                ).GetArticles("penguins", int.MaxValue)
            );
        }
    }
}
