using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FullTextIndexer.Common.Lists;
using FullTextIndexer.Common.Logging;
using FullTextIndexer.Core.Indexes;
using FullTextIndexer.Core.Indexes.TernarySearchTree;
using FullTextIndexer.Core.TokenBreaking;
using FullTextIndexer.Serialisation.Json;

namespace Tester
{
    class Program
	{
		static void Main()
		{
			var posts = PostDataLoader.GetFromLocalSqliteFile(GetExampleBlogDatabaseFile());
			var productIndexGenerator = new PostIndexGenerator(
				tokenBreaker: new WhiteSpaceExtendingTokenBreaker(
					charsToTreatAsWhitespace: new ImmutableList<char>('<', '>', '[', ']', '(', ')', '{', '}', '.', ','),
					tokenBreaker: new WhiteSpaceTokenBreaker()
				),
				sourceStringComparer: new EnglishPluralityStringNormaliser(
					optionalPreNormaliser: DefaultStringNormaliser.Instance,
					preNormaliserWork:
						EnglishPluralityStringNormaliser.PreNormaliserWorkOptions.PreNormaliserLowerCases |
						EnglishPluralityStringNormaliser.PreNormaliserWorkOptions.PreNormaliserTrims
				),
				logger: new ConsoleLogger()
			);
			var index = productIndexGenerator.Generate(posts);

			Console.WriteLine("Serialise with IndexDataJsonSerialiser");
			byte[] serialisedData;
			using (var stream = new MemoryStream())
			{
				var jsonTimer = Stopwatch.StartNew();
				IndexDataJsonSerialiser.Serialise(index, stream);
				serialisedData = stream.ToArray();
				jsonTimer.Stop();
				Console.WriteLine("- Time taken to serialise: " + jsonTimer.ElapsedMilliseconds + "ms");
			}
			using (var stream = new MemoryStream(serialisedData))
			{
				var jsonTimer = Stopwatch.StartNew();
				var clonedIndex = IndexDataJsonSerialiser.Deserialise<int>(stream);
				jsonTimer.Stop();
				Console.WriteLine("- Time taken to deserialise: " + jsonTimer.ElapsedMilliseconds + "ms");
			}
			Console.WriteLine();

			// This will look for Posts with "Immutable" in any of the fields (Title, Body, etc..)
			var matchesOverSingleField = index.GetPartialMatches(
				"Immutable",
				new WhiteSpaceExtendingTokenBreaker(
					new ImmutableList<char>('<', '>', '[', ']', '(', ')', '{', '}', '.', ','),
					new WhiteSpaceTokenBreaker()
				)
			);
			Console.WriteLine("Search for \"Immutable\"");
			Console.WriteLine();
			foreach (var match in matchesOverSingleField.OrderByDescending(match => match.Weight))
				Console.WriteLine(posts.First(post => post.Id == match.Key).Title);
			Console.WriteLine();

			// This will look for Posts that have both "Penguin" and "Dan" somewhere in their content, not necessarily in the same field
			Console.WriteLine("Search for \"Penguin Dan\" (identify Posts that contain both words somewhere in their searchable fields)");
			Console.WriteLine();
			var matchesOverMultipleFields = index.GetPartialMatches(
				"Penguin Dan",
				new WhiteSpaceExtendingTokenBreaker(
					new ImmutableList<char>('<', '>', '[', ']', '(', ')', '{', '}', '.', ','),
					new WhiteSpaceTokenBreaker()
				)
			);
			foreach (var match in matchesOverMultipleFields.OrderByDescending(match => match.Weight))
				Console.WriteLine(posts.First(post => post.Id == match.Key).Title);
			Console.WriteLine();

			Console.WriteLine("Press [Enter] to terminate..");
			Console.ReadLine();
		}

		private static FileInfo GetExampleBlogDatabaseFile()
		{
			// The "Blog.sqlite" file comes from the "TesterShared" Shared Project and will be dropped into the build folder in a regular .NET console application since it's marked
			// as "Content" / "Copy if newer" in the Shared Project. However, Shared Projects aren't supported by .NET Core projects and so some workarounds are required in the
			// project.json, one of which is copying the database file from "TesterShared" - but it doesn't seem possible to configure the "copyToOutput" option to put the
			// file into the root of the output folder, it always puts it into a "TesterShared" folder (there are "mapping" options but I can't make them do what I want,
			// I don't know if they're even supposed to do what I want). So, as a compromise, we'll just try to find the first "Blog.sqlite" file in the current folder
			// or in any subfolder. Note that the .NET Core app will consider the base folder to be the project root and not the location of the executable, so it's
			// possible that we could build in release configuration and then load the database file from the debug output folder but I've spent long enough on
			// this that I've given up, this Shared Project approach is hacks-on-hacks anyway.
			var file = new DirectoryInfo(".").EnumerateFiles("Blog.sqlite", SearchOption.AllDirectories).FirstOrDefault();
			if (file == null)
				throw new Exception("Unable to locate \"Blog.sqlite\" database file");
			return file;
		}
	}
}
