using System;

namespace FullTextIndexer.Common.Logging
{
	/// <summary>
	/// These are listed in order of increasing severity
	/// </summary>
#if NET45
    [Serializable]
#endif
	public enum LogLevel
	{
		Debug,
		Info,
		Warning,
		Error
	}
}
