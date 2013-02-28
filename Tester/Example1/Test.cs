﻿using System;
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
            // TODO: Plurality handling??

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
                "Fear Moon Exercise, Boston",
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
