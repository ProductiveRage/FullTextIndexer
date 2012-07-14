// TODO: Remove all these
using System;
using System.Linq;
using System.Collections.Generic;
using Common.Lists;
using Common.Logging;

namespace Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            var searchTermMatches = new List<Tuple<string, List<int>>>
            {
                Tuple.Create("the", new List<int> { 1, 2, 3}),
                Tuple.Create("search", new List<int> { 4, 5}),
                Tuple.Create("terms", new List<int> { 6, 7, 8})
            };

            var allPermutations = (new[] { -1 }).Concat(searchTermMatches[0].Item2).Select(v => new[] { v }).ToArray();
            for (var index = 1; index < searchTermMatches.Count; index++)
            {
                allPermutations = allPermutations
                    .SelectMany(a11 => (new[] { -1 }).Concat(searchTermMatches[index].Item2).Select(v => a11.Concat(new[] { v }).ToArray()))
                    .ToArray();
            }
         }

        static void Main3333(string[] args)
        {
            var searchTermMatches = new List<Tuple<string, List<int>>>
            {
                Tuple.Create("the", new List<int> { 1, 2, 3}),
                Tuple.Create("search", new List<int> { 4, 5}),
                Tuple.Create("terms", new List<int> { 6, 7, 8})
            };

            
            
            
            var matchIndices = new int[searchTermMatches.Count];
            for (var index = 0; index < matchIndices.Length; index++)
                matchIndices[index] = -1;

            var allPermutations = new List<int[]>();
            while (true)
            {
                var permutation = new int[searchTermMatches.Count];
                for (var index = 0; index < searchTermMatches.Count; index++)
                {
                    var matchIndex = matchIndices[index];
                    if (matchIndex == -1)
                        permutation[index] = -1;
                    else
                        permutation[index] = searchTermMatches[index].Item2[matchIndex];
                }
                allPermutations.Add(permutation);

                var indexToIncrement = matchIndices.Length - 1;
                while (true)
                {
                    matchIndices[indexToIncrement]++;

                    // If not pushed the current entry passed its final element then we've generated a valid permutation that needs adding to the list
                    if (matchIndices[indexToIncrement] < searchTermMatches[indexToIncrement].Item2.Count)
                        break;

                    // Otherwise we'll need to reset the current index to -1 and increment the index before it UNLESS we're on the very first index
                    // which means there is no index before it (and so the permutation generation work will be complete)
                    if (indexToIncrement == 0)
                        break;
                    
                    matchIndices[indexToIncrement] = -1;
                    indexToIncrement--;
                }
                if (matchIndices[0] >= searchTermMatches[0].Item2.Count)
                    break;
            }
        }

        /*
        public static object ReadFromDisk(string filename)
        {
            object obj;
            using (var stream = System.IO.File.Open(filename, System.IO.FileMode.Open))
            {
                var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                obj = formatter.Deserialize(stream);
            }
            return obj;
        }
         */

        static void Main2(string[] args)
        {
            /*
            var f = @"F:\Documents and Settings\Dan\My Documents\Dropbox\Blog\Blog\App_Data\SearchIndex.dat";
            var e = new FullTextIndexer.Indexes.TernarySearchTree.EnglishPluralityStringNormaliser(
                new FullTextIndexer.Indexes.TernarySearchTree.DefaultStringNormaliser(),
                FullTextIndexer.Indexes.TernarySearchTree.EnglishPluralityStringNormaliser.PreNormaliserWorkOptions.PreNormaliserLowerCases
                | FullTextIndexer.Indexes.TernarySearchTree.EnglishPluralityStringNormaliser.PreNormaliserWorkOptions.PreNormaliserTrims
            );
            using (var stream2 = System.IO.File.Open(f, System.IO.FileMode.Create))
            {
                new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter().Serialize(stream2, e);
            }
            var e2 = ReadFromDisk(f);
             */

            // TODO: Remove
            (new BlogExample(
                new FullTextIndexer.TokenBreaking.WhiteSpaceExtendingTokenBreaker(
                    new ImmutableList<char>(new[] { '<', '>', '[', ']', '(', ')', '{', '}', '.', ',' }),
                    new FullTextIndexer.TokenBreaking.WhiteSpaceTokenBreaker()
                ),
                new FullTextIndexer.Indexes.TernarySearchTree.EnglishPluralityStringNormaliser(
                    new FullTextIndexer.Indexes.TernarySearchTree.DefaultStringNormaliser(),
                    FullTextIndexer.Indexes.TernarySearchTree.EnglishPluralityStringNormaliser.PreNormaliserWorkOptions.PreNormaliserLowerCases
                    | FullTextIndexer.Indexes.TernarySearchTree.EnglishPluralityStringNormaliser.PreNormaliserWorkOptions.PreNormaliserTrims
                ),
                new ConsoleLogger()
            )).Go();

            Example1.Test.Go();
            Example2.Test.Go();
            Example3.Test.Go();

            System.Console.ReadLine();
        }

        private static TimeSpan TimeNormalisation(Func<FullTextIndexer.Indexes.TernarySearchTree.IStringNormaliser> stringNormaliserGenerator, IEnumerable<string> values, int repeatCount)
        {
            if (stringNormaliserGenerator == null)
                throw new ArgumentNullException("stringNormaliserGenerator");
            if (values == null)
                throw new ArgumentNullException("values");
            if (repeatCount <= 0)
                throw new ArgumentOutOfRangeException("repeatCount");

            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            var stringNormaliser = stringNormaliserGenerator();
            timer.Stop();
            for (var index = 0; index < repeatCount; index++)
            {
                timer.Start();
                foreach (var value in values)
                    stringNormaliser.GetNormalisedString(value);
                timer.Stop();
                GC.Collect(2, GCCollectionMode.Forced);
                GC.Collect(2, GCCollectionMode.Forced);
            }
            return timer.Elapsed;
        }
    }
}
