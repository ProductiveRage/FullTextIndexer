using System;
using System.Collections.Generic;
using FullTextIndexer.Common.Lists;
using FullTextIndexer.Core.Indexes;

namespace UnitTests.Querier.QueryTranslators
{
	public class WeightedEntrySetEqualityComparer<TKey> : IEqualityComparer<NonNullImmutableList<WeightedEntry<TKey>>>
	{
		private readonly IEqualityComparer<TKey> _keyComparer;
		public WeightedEntrySetEqualityComparer(IEqualityComparer<TKey> keyComparer)
		{
			if (keyComparer == null)
				throw new ArgumentNullException("keyComparer");

			_keyComparer = keyComparer;
		}

		public bool Equals(NonNullImmutableList<WeightedEntry<TKey>> x, NonNullImmutableList<WeightedEntry<TKey>> y)
		{
			if (x == null)
				throw new ArgumentNullException("x");
			if (y == null)
				throw new ArgumentNullException("y");

			if (x.Count != y.Count)
				return false;

			for (var index = 0; index < x.Count; index++)
			{
				if (!_keyComparer.Equals(x[index].Key, y[index].Key) || (x[index].Weight != y[index].Weight))
					return false;
			}
			return true;
		}

		public int GetHashCode(NonNullImmutableList<WeightedEntry<TKey>> obj)
		{
			if (obj == null)
				throw new ArgumentNullException("obj");

			return 0;
		}
	}
}
