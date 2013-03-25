using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FullTextIndexer.Common.Lists;
using FullTextIndexer.Common.Logging;
using FullTextIndexer.Core.Indexes;
using FullTextIndexer.Core.Indexes.TernarySearchTree;
using FullTextIndexer.Core.TokenBreaking;
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

            var matchesOverSingleField = index.GetPartialMatches(
                "Exercise",
                new WhiteSpaceExtendingTokenBreaker(
                    new ImmutableList<char>(new[] { '<', '>', '[', ']', '(', ')', '{', '}', '.', ',' }),
                    new WhiteSpaceTokenBreaker()
                )
            );

            var matchesOverMultipleFields = index.GetPartialMatches(
                "Penguins Slap Christopher",
                new WhiteSpaceExtendingTokenBreaker(
                    new ImmutableList<char>(new[] { '<', '>', '[', ']', '(', ')', '{', '}', '.', ',' }),
                    new WhiteSpaceTokenBreaker()
                )
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
