using System;

namespace FullTextIndexer.Common.Logging
{
	/// <summary>
	/// Write log messages to trace, formatted to include log level, date, time and exception details (message and stack trace) if non-null
	/// </summary>
#if NET45
    [Serializable]
#endif
	public class ConsoleLogger : TextWriterLogger
	{
		public ConsoleLogger() : base(content => { Console.WriteLine(content); }, ExceptionStackDisplayOptions.Show) { }
	}
}
