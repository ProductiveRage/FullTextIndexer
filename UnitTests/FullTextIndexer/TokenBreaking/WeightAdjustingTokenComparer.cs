﻿using System;
using System.Collections.Generic;
using FullTextIndexer.Core.TokenBreaking;

namespace UnitTests.FullTextIndexer.TokenBreaking
{
	public class WeightAdjustingTokenComparer : IEqualityComparer<WeightAdjustingToken>
	{
		public bool Equals(WeightAdjustingToken x, WeightAdjustingToken y)
		{
			if (x == null)
				throw new ArgumentNullException(nameof(x));
			if (y == null)
				throw new ArgumentNullException(nameof(y));

			return (
				(x.Token == y.Token) &&
				(x.SourceLocation.TokenIndex == y.SourceLocation.TokenIndex) &&
				(x.SourceLocation.SourceIndex == y.SourceLocation.SourceIndex) &&
				(x.SourceLocation.SourceTokenLength == y.SourceLocation.SourceTokenLength) &&
				(x.WeightMultiplier == y.WeightMultiplier)
			);
		}

		public int GetHashCode(WeightAdjustingToken obj)
		{
			if (obj == null)
				throw new ArgumentNullException(nameof(obj));

			return 0;
		}
	}
}
