using System;
using FullTextIndexer.Common.Lists;

namespace FullTextIndexer.Common.Logging
{
#if NET45
    [Serializable]
#endif
	public class CombinedLogger : ILogger
	{
		private NonNullImmutableList<ILogger> _loggers;
		public CombinedLogger(params ILogger[] loggers)
		{
			if (loggers == null)
				throw new ArgumentNullException("loggers");

			_loggers = loggers.ToNonNullImmutableList();
		}

		/// <summary>
		/// This will throw an exception if issues are encountered - this includes cases of null or empty content (Exception is optional and so may be null)
		/// </summary>
		public void Log(LogLevel logLevel, DateTime logDate, Func<string> contentGenerator, Exception exception)
		{
			if (!Enum.IsDefined(typeof(LogLevel), logLevel))
				throw new ArgumentOutOfRangeException("logLevel");
			if (contentGenerator == null)
				throw new ArgumentNullException("contentGenerator");

			foreach (var logger in _loggers)
				logger.Log(logLevel, logDate, contentGenerator, exception);
		}
	}
}
