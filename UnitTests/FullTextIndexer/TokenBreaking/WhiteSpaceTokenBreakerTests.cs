using System;
using System.Linq;
using FullTextIndexer.Common.Lists;
using FullTextIndexer.Core.TokenBreaking;
using Xunit;

namespace UnitTests.FullTextIndexer.TokenBreaking
{
	public class WhiteSpaceTokenBreakerTests
	{
		[Fact]
		public void StraightDownTheMiddleTest()
		{
			var value = " one    two\r\nthree four ";
			var expected = new[]
			{
				new WeightAdjustingToken("one", 0, 1, 3, 1),
				new WeightAdjustingToken("two", 1, 8, 3, 1),
				new WeightAdjustingToken("three", 2, 13, 5, 1),
				new WeightAdjustingToken("four", 3, 19, 4, 1)
			};

			Assert.Equal<WeightAdjustingToken>(
				expected,
				new WhiteSpaceTokenBreaker().Break(value),
				new WeightAdjustingTokenComparer()
			);
		}

		[Fact]
		public void SourceIndexValuesShouldBeCumulativeIfAWrappedTokenBreakerIsSpecified()
		{
			var value = " lower-cased  content ";
			var expected = new[]
			{
				new WeightAdjustingToken("lower", 0, 1, 5, 1),
				new WeightAdjustingToken("cased", 1, 7, 5, 1),
				new WeightAdjustingToken("content", 2, 14, 7, 1)
			};

			Assert.Equal<WeightAdjustingToken>(
				expected,
				new WhiteSpaceTokenBreaker(new SingleHyphenBreakingTokenBreaker()).Break(value),
				new WeightAdjustingTokenComparer()
			);
		}

		private class SingleHyphenBreakingTokenBreaker : ITokenBreaker
		{
			public NonNullImmutableList<WeightAdjustingToken> Break(string value)
			{
				if (value == null)
					throw new ArgumentNullException("value");

				var splitPoint = value.Select((c, index) => new { Character = c, Index = index }).Where(c => c.Character == '-').Select(c => c.Index).Single();
				return new NonNullImmutableList<WeightAdjustingToken>(new[]
				{
					new WeightAdjustingToken(value.Substring(0, splitPoint), 0, 0, splitPoint, 1),
					new WeightAdjustingToken(value.Substring(splitPoint + 1), 1, splitPoint + 1, value.Length - (splitPoint + 1), 1)
				});
			}
		}
	}
}
