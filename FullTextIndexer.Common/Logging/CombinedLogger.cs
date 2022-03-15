using System;
using FullTextIndexer.Common.Lists;

namespace FullTextIndexer.Common.Logging
{
    [Serializable]
	public class CombinedLogger : ILogger
	{
		private readonly NonNullImmutableList<ILogger> _loggers;
		public CombinedLogger(params ILogger[] loggers)
		{
			if (loggers == null)
				throw new ArgumentNullException(nameof(loggers));

			_loggers = loggers.ToNonNullImmutableList();
		}

		/// <summary>
		/// This will throw an exception if issues are encountered - this includes cases of null or empty content (Exception is optional and so may be null)
		/// </summary>
		public void Log(LogLevel logLevel, DateTime logDate, Func<string> contentGenerator, Exception exception)
		{
			if (!Enum.IsDefined(typeof(LogLevel), logLevel))
				throw new ArgumentOutOfRangeException(nameof(logLevel));
			if (contentGenerator == null)
				throw new ArgumentNullException(nameof(contentGenerator));

			foreach (var logger in _loggers)
				logger.Log(logLevel, logDate, contentGenerator, exception);
		}
	}
}
