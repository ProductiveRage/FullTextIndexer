using System;
using System.Linq;
using FullTextIndexer.Common.Lists;
using FullTextIndexer.Core.TokenBreaking;
using Xunit;

namespace UnitTests.FullTextIndexer.TokenBreaking
{
	public class ConsecutiveTokenCombiningTokenBreakerTests
	{
		/// <summary>
		/// The length of the combined content should not just be the sum of the lengths of the combined tokens since this wouldn't take into account any
		/// breaking characters (such as whitespace, depending upon the wrapped token breaker) between the tokens
		/// </summary>
		[Fact]
		public void CombinedTokensShouldTakeTheIndexOfTheFirstTokenAndTheLengthOfTheEntireSourceSegment()
		{
			var content = "one two three";
			var whiteSpaceBrokenTokens = new NonNullImmutableList<WeightAdjustingToken>(new[]
			{
				new WeightAdjustingToken("one", 0, 0, 3, 1),
				new WeightAdjustingToken("two", 1, 4, 3, 1),
				new WeightAdjustingToken("three", 2, 8, 5, 1)
			});

			var expected = new NonNullImmutableList<WeightAdjustingToken>(new[]
			{
				new WeightAdjustingToken("one", 0, 0, 3, 1),
				new WeightAdjustingToken("two", 1, 4, 3, 1),
				new WeightAdjustingToken("three", 2, 8, 5, 1),
				new WeightAdjustingToken("one two", 0, 0, 7, 1),
				new WeightAdjustingToken("two three", 1, 4, 9, 1),
				new WeightAdjustingToken("one two three", 0, 0, 13, 1)
			});
			
			Assert.Equal<WeightAdjustingToken>(
				expected,
				new ConsecutiveTokenCombiningTokenBreaker(
					new FixedContentTokenBreaker(content, whiteSpaceBrokenTokens),
					3,
					weightMultipliersOfCombinedTokens => weightMultipliersOfCombinedTokens.Average()
				).Break(content),
				new WeightAdjustingTokenComparer()
			);
		}

		private class FixedContentTokenBreaker : ITokenBreaker
		{
			private readonly string _expectedValue;
			private readonly NonNullImmutableList<WeightAdjustingToken> _results;
			public FixedContentTokenBreaker(string expectedValue, NonNullImmutableList<WeightAdjustingToken> results)
			{
				if (expectedValue == null)
					throw new ArgumentNullException("expectedValue");
				if (results == null)
					throw new ArgumentNullException("results");

				_expectedValue = expectedValue;
				_results = results;

			}

			public NonNullImmutableList<WeightAdjustingToken> Break(string value)
			{
				if (value == null)
					throw new ArgumentNullException("value");
				
				if (value != _expectedValue)
					throw new ArgumentException("This FixedContentTokenBreaker instance is only configured to Break the value: \"" + _expectedValue + "\"");

				return _results;
			}
		}
	}
}
