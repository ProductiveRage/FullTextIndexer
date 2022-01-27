using System;
using System.Linq;
using FullTextIndexer.Common.Lists;
using FullTextIndexer.Core.Indexes;
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
			var whiteSpaceBrokenTokens = NonNullImmutableList.Create(
				new WeightAdjustingToken("one", 1, new SourceLocation(0, 0, 3)),
				new WeightAdjustingToken("two", 1, new SourceLocation(1, 4, 3)),
				new WeightAdjustingToken("three", 1, new SourceLocation(2, 8, 5))
			);

			var expected = NonNullImmutableList.Create(
				new WeightAdjustingToken("one", 1, new SourceLocation(0, 0, 3)),
				new WeightAdjustingToken("two", 1, new SourceLocation(1, 4, 3)),
				new WeightAdjustingToken("three", 1, new SourceLocation(2, 8, 5)),
				new WeightAdjustingToken("one two", 1, new SourceLocation(0, 0, 7)),
				new WeightAdjustingToken("two three", 1, new SourceLocation(1, 4, 9)),
				new WeightAdjustingToken("one two three", 1, new SourceLocation(0, 0, 13))
			);
			
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
                _expectedValue = expectedValue ?? throw new ArgumentNullException("expectedValue");
				_results = results ?? throw new ArgumentNullException("results");

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
