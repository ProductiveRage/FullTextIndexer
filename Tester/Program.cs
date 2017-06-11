namespace Tester
{
	class Program
	{
		static void Main(string[] args)
		{
			// Run the demonstration code (from "Example.cs" in the TesterShared project) - this will load data from a Sqlite file, build an index,
			// serialise it, deserialise it then run a couple of queries against it (this Test project demonstrates it working in .NET Core)
			Example.Go();
		}
	}
}
