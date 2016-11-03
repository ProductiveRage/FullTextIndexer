using System;
using System.Linq;
using FullTextIndexer.Common.Lists;
using FullTextIndexer.Common.Logging;
using FullTextIndexer.Core.Indexes;
using FullTextIndexer.Core.Indexes.TernarySearchTree;
using FullTextIndexer.Core.TokenBreaking;

namespace Tester
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Loading data from Sqlite file and building index..");
			var posts = PostDataLoader.GetFromLocalSqliteFile();
			var productIndexGenerator = new PostIndexGenerator(
				new WhiteSpaceExtendingTokenBreaker(
					new ImmutableList<char>('<', '>', '[', ']', '(', ')', '{', '}', '.', ','),
					new WhiteSpaceTokenBreaker()
				),
				new DefaultStringNormaliser(),
				new ConsoleLogger()
			);
			var index = productIndexGenerator.Generate(posts);
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
	}
}
