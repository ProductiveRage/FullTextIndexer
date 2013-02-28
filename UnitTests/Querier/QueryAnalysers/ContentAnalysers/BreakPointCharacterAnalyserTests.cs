using System;
using System.Collections.Generic;
using System.Linq;
using FullTextIndexer.Querier.QueryAnalysers.ContentAnalysers;
using FullTextIndexer.Querier.QueryAnalysers.StringNavigators;
using FullTextIndexer.Querier.QuerySegments;
using Xunit;
using FullTextIndexer.Common.Lists;

namespace UnitTests.Querier.QueryAnalysers.ContentAnalysers
{
	public class BreakPointCharacterAnalyserTests
	{
		[Fact]
		public void LeadingAndTrailingWhitespaceShouldBeIgnored()
		{
			var content = " test ";
			var expected = new StandardMatchQuerySegment("test");

			Assert.Equal<IQuerySegment>(
				expected,
				Process(content),
				new QuerySegmentComparer()
			);
		}

		[Fact]
		public void QuotedSectionsDoNotEndOnWhitespace()
		{
			var content = "\"test test\"";
			var expected = new PreciseMatchQuerySegment("test test");

			Assert.Equal<IQuerySegment>(
				expected,
				Process(content),
				new QuerySegmentComparer()
			);
		}

		[Fact]
		public void QuotedSectionsSupportQuoteEscaping()
		{
			var content = "\"test\\\"test\"";
			var expected = new PreciseMatchQuerySegment("test\"test");

			Assert.Equal<IQuerySegment>(
				expected,
				Process(content),
				new QuerySegmentComparer()
			);
		}

		[Fact]
		public void NestedBracketsAreSupported()
		{
			var content = "test0 (test1 (test2 test3)) test4";
			var expected = NewCombiningQuerySegment(
				new StandardMatchQuerySegment("test0"),
				NewCombiningQuerySegment(
					new StandardMatchQuerySegment("test1"),
					NewCombiningQuerySegment(
						new StandardMatchQuerySegment("test2"),
						new StandardMatchQuerySegment("test3")
					)
				),
				new StandardMatchQuerySegment("test4")
			);

			Assert.Equal<IQuerySegment>(
				expected,
				Process(content),
				new QuerySegmentComparer()
			);
		}

		[Fact]
		public void BracketsDoNotNeedSurroundingWhitespace()
		{
			var content = "test0(test1 test2)";
			var expected = NewCombiningQuerySegment(
				new StandardMatchQuerySegment("test0"),
				NewCombiningQuerySegment(
					new StandardMatchQuerySegment("test1"),
					new StandardMatchQuerySegment("test2")
				)
			);

			Assert.Equal<IQuerySegment>(
				expected,
				Process(content),
				new QuerySegmentComparer()
			);
		}

		[Fact]
		public void BracketsCanBeEscaped()
		{
			var content = "test0\\(test1 test2\\)";
			var expected = NewCombiningQuerySegment(
				new StandardMatchQuerySegment("test0(test1"),
				new StandardMatchQuerySegment("test2)")
			);

			Assert.Equal<IQuerySegment>(
				expected,
				Process(content),
				new QuerySegmentComparer()
			);
		}

		[Fact]
		public void CompulsoryContentMarkersMayBeCombinedWithQuotedSections()
		{
			var content = "+\"test0 test1\"";
			var expected = new CompulsoryQuerySegment(
				new PreciseMatchQuerySegment("test0 test1")
			);

			Assert.Equal<IQuerySegment>(
				expected,
				Process(content),
				new QuerySegmentComparer()
			);
		}

		[Fact]
		public void CompulsoryContentMarkersMayBeCombinedWithBrackets()
		{
			var content = "+(test0 test1)";
			var expected = new CompulsoryQuerySegment(
				NewCombiningQuerySegment(
					new StandardMatchQuerySegment("test0"),
					new StandardMatchQuerySegment("test1")
				)
			);

			Assert.Equal<IQuerySegment>(
				expected,
				Process(content),
				new QuerySegmentComparer()
			);
		}

		private IQuerySegment NewCombiningQuerySegment(params IQuerySegment[] segments)
		{
			if (segments == null)
				throw new ArgumentNullException("segments");

			return new CombiningQuerySegment(segments.ToNonNullImmutableList());
		}

		private IQuerySegment Process(string content)
		{
			if (content == null)
				throw new ArgumentNullException("content");

			return (new BreakPointCharacterAnalyser()).Process(new StringNavigator(content)).QuerySegment;
		}

		private class QuerySegmentComparer : IEqualityComparer<IQuerySegment>
		{
			public bool Equals(IQuerySegment x, IQuerySegment y)
			{
				if (x == null)
					throw new ArgumentNullException("x");
				if (y == null)
					throw new ArgumentNullException("y");

				if (x.GetType() != y.GetType())
					return false;

				if (x is NoMatchContentQuerySegment)
					return true;

				var combiningQuerySegmentX = x as CombiningQuerySegment;
				var combiningQuerySegmentY = y as CombiningQuerySegment;
				if (combiningQuerySegmentX != null)
				{
					var combinedSegmentsX = combiningQuerySegmentX.Segments.ToArray();
					var combinedSegmentsY = combiningQuerySegmentY.Segments.ToArray();
					if (combinedSegmentsX.Length != combinedSegmentsY.Length)
						return false;
					for (var index = 0; index < combinedSegmentsX.Length; index++)
					{
						if (!Equals(combinedSegmentsX[index], combinedSegmentsY[index]))
							return false;
					}
					return true;
				}

				var excludingQuerySegmentX = x as ExcludingQuerySegment;
				var excludingQuerySegmentY = y as ExcludingQuerySegment;
				if (excludingQuerySegmentX != null)
					return Equals(excludingQuerySegmentX.Segment, excludingQuerySegmentY.Segment);

				var compulsoryQuerySegmentX = x as CompulsoryQuerySegment;
				var compulsoryQuerySegmentY = y as CompulsoryQuerySegment;
				if (compulsoryQuerySegmentX != null)
					return Equals(compulsoryQuerySegmentX.Segment, compulsoryQuerySegmentY.Segment);

				var preciseQuerySegmentX = x as PreciseMatchQuerySegment;
				var preciseQuerySegmentY = y as PreciseMatchQuerySegment;
				if (preciseQuerySegmentX != null)
					return preciseQuerySegmentX.Value == preciseQuerySegmentY.Value;

				var standardQuerySegmentX = x as StandardMatchQuerySegment;
				var standardQuerySegmentY = y as StandardMatchQuerySegment;
				if (standardQuerySegmentX != null)
					return standardQuerySegmentX.Value == standardQuerySegmentY.Value;

				throw new NotSupportedException("Unsupported IQuerySegment implementation: " + x.GetType());
			}

			public int GetHashCode(IQuerySegment obj)
			{
				// Always returning zero here will mean that the Equals method will be called for every comparison, which is what we want!
				return 0;
			}
		}
	}
}
