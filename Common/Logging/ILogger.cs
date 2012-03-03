using System;

namespace Common.Logging
{
	public interface ILogger
	{
		/// <summary>
		/// This will throw an exception if issues are encountered - this includes cases of null or empty content (Exception is optional and so may be null),
		/// logDate is specified so that asynchronous or postponed logging can be implemented
		/// </summary>
		void Log(LogLevel logLevel, DateTime logDate, Func<string> contentGenerator, Exception exception);
	}
}
