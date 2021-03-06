﻿using System;

namespace FullTextIndexer.Common.Logging
{
    /// <summary>
    /// These are listed in order of increasing severity
    /// </summary>
    [Serializable]
	public enum LogLevel
	{
		Debug,
		Info,
		Warning,
		Error
	}
}
