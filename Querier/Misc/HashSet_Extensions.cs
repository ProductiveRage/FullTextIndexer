﻿using System;
using System.Collections.Generic;

namespace FullTextIndexer.Querier.Misc
{
	public static class HashSet_Extensions
	{
		public static void AddRange<T>(this HashSet<T> source, IEnumerable<T> values)
		{
			if (source == null)
				throw new ArgumentNullException("source");
			if (values == null)
				throw new ArgumentNullException("values");

			foreach (var value in values)
				source.Add(value);
		}
	}
}
