namespace Tester
{
	class Program
	{
		static void Main(string[] args)
		{
			var x = new FullTextIndexer.Common.Lists.NonNullImmutableList<string>(new[] { "a", "b" });
			var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
			using (var stream = new System.IO.MemoryStream())
			{
				formatter.Serialize(stream, x);
			}




			// Run the demonstration code (from "Example.cs" in the TesterShared project) - this will load data from a Sqlite file, build an index,
			// serialise it, deserialise it then run a couple of queries against it (this Test project demonstrates it working in .NET 4.5.2)
			Example.Go();
		}
	}
}
