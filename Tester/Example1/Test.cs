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
using Tester.Example1.SourceData;

namespace Tester.Example1
{
    public static class Test
    {
        public static void Go()
        {
            var dataFile = new FileInfo("SampleData-Example1.dat");
            if (!dataFile.Exists)
                GenerateDataFile(dataFile, "server=.\\SQLExpress;database=Pub;Trusted_Connection=True;");

            var data = Serialisation.ReadFromDisk<NonNullImmutableList<Product>>(dataFile);
            var productIndexGenerator = new ProductIndexGenerator(
                new WhiteSpaceExtendingTokenBreaker(
                    new ImmutableList<char>(new[] { '<', '>', '[', ']', '(', ')', '{', '}', '.', ',' }),
                    new WhiteSpaceTokenBreaker()
                ),
                new DefaultStringNormaliser(),
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
                "Fear Moon Exercise, Boston",
                new WhiteSpaceExtendingTokenBreaker(
                    new ImmutableList<char>(new[] { '<', '>', '[', ']', '(', ')', '{', '}', '.', ',' }),
                    new WhiteSpaceTokenBreaker()
                )
            );
        }

        private static void GenerateDataFile(FileInfo dataFile, string connectionString)
        {
            if (dataFile == null)
                throw new ArgumentNullException("dataFile");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Null/empty connectionString specified");

            Serialisation.WriteToDisk(
                dataFile,
                new PubDataLoader(connectionString).GetProducts()
            );
        }
    }
}
