using System;
using System.Collections.Generic;
using FullTextIndexer.Core.TokenBreaking;

namespace UnitTests.FullTextIndexer.TokenBreaking
{
	public class WeightAdjustingTokenComparer : IEqualityComparer<WeightAdjustingToken>
	{
		public bool Equals(WeightAdjustingToken x, WeightAdjustingToken y)
		{
			if (x == null)
				throw new ArgumentNullException("x");
			if (y == null)
				throw new ArgumentNullException("y");

			return (
				(x.Token == y.Token) &&
				(x.SourceIndex == y.SourceIndex) &&
				(x.SourceTokenLength == y.SourceTokenLength) &&
				(x.WeightMultiplier == y.WeightMultiplier)
			);
		}

		public int GetHashCode(WeightAdjustingToken obj)
		{
			if (obj == null)
				throw new ArgumentNullException("obj");

			return 0;
		}
	}
}
