using System;

namespace FullTextIndexer.Common.Logging
{
	/// <summary>
	/// This logger does nothing, it doesn't even validate parameters
	/// </summary>
	public class NullLogger : ILogger
	{
		public void Log(LogLevel logLevel, DateTime logDate, Func<string> contentGenerator, Exception exception) { }
	}
}
