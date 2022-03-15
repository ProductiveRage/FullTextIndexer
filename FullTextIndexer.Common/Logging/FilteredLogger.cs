using System;
using FullTextIndexer.Common.Lists;

namespace FullTextIndexer.Common.Logging
{
    /// <summary>
    /// Write log messages to trace, including additional content such as date, time and thread id
    /// </summary>
    [Serializable]
	public class FilteredLogger : ILogger
	{
		private readonly ILogger _logger;
		private readonly ImmutableList<LogLevel> _allowedLogLevels;
		public FilteredLogger(ILogger logger, params LogLevel[] allowedLogLevels)
		{
            if (allowedLogLevels == null)
				throw new ArgumentNullException(nameof(allowedLogLevels));

			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_allowedLogLevels = allowedLogLevels.ToImmutableList(new LogLevelValueValidator());
		}

		private class LogLevelValueValidator : IValueValidator<LogLevel>
		{
			public void EnsureValid(LogLevel value)
			{
				if (!Enum.IsDefined(typeof(LogLevel), value))
					throw new ArgumentOutOfRangeException(nameof(value), "Specified LogLevel value was invalid");
			}
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

			if (_allowedLogLevels.Contains(logLevel))
				_logger.Log(logLevel, logDate, contentGenerator, exception);
		}
	}
}
