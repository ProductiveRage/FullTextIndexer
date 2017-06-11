using System;
using System.Text;

namespace FullTextIndexer.Common.Logging
{
	/// <summary>
	/// Write log messages to a text output, formatted to include log level, date, time and exception details (message and stack trace) if non-null
	/// </summary>
#if NET45
    [Serializable]
#endif
	public abstract class TextWriterLogger : ILogger
	{
		private Action<string> _writer;
		private ExceptionStackDisplayOptions _exceptionStackDisplay;
		public TextWriterLogger(Action<string> writer, ExceptionStackDisplayOptions exceptionStackDisplay)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");
			if (!Enum.IsDefined(typeof(ExceptionStackDisplayOptions), exceptionStackDisplay))
				throw new ArgumentOutOfRangeException("exceptionStackDisplay");

			_writer = writer;
			_exceptionStackDisplay = exceptionStackDisplay;
		}

		public enum ExceptionStackDisplayOptions
		{
			Hide,
			Show
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
			var content = contentGenerator();
			if (string.IsNullOrWhiteSpace(content))
				throw new ArgumentException("Null/empty content specified");

			var detailedContent = new StringBuilder();
			detailedContent.AppendFormat("[{0}] ", logDate.ToString("yyyy-MM-dd HH:mm:ss.fff"));
			if (logLevel != LogLevel.Info)
			{
				// Don't bother displaying the text "Info", it's redundant (Debug, Warning or Error are useful content, though)
				detailedContent.AppendFormat("[{0}] ", logLevel.ToString());
			}
			detailedContent.Append(content);
			if (exception != null)
			{
				detailedContent.AppendFormat(" - {0}", exception.Message);
				if (_exceptionStackDisplay == ExceptionStackDisplayOptions.Show)
				{
					detailedContent.AppendLine();
					detailedContent.Append(exception.StackTrace);
				}
			}
			_writer(detailedContent.ToString());
		}
	}
}
