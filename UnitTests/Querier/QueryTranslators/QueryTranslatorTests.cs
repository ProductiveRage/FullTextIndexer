using System;
using System.Linq;
using FullTextIndexer.Common.Lists;
using FullTextIndexer.Core.Indexes;
using FullTextIndexer.Core.IndexGenerators;
using FullTextIndexer.Querier.QuerySegments;
using FullTextIndexer.Querier.QueryTranslators;
using Xunit;

namespace UnitTests.Querier.QueryTranslators
{
	public class QueryTranslatorTests
	{
		[Fact]
		public void PreciseMatchQuerySegmentsUseThePreciseMatchIndex()
		{
			var queryTranslator = new QueryTranslator<int>(
				new TestIntKeyIndexDataBuilder().Add("test0", 1, 0.1f, new SourceFieldLocation(0, 0, 0, 5, 0.1f)).Get(),
				new TestIntKeyIndexDataBuilder().Add("test0", 2, 0.1f, new SourceFieldLocation(0, 0, 0, 5, 0.1f)).Get(),
				(matchWeights, sourceQuerySegments) => matchWeights.Sum()
			);

			// If the standardMatchIndexData was used then we won't get only the entry for Key 2 / Weight 0.1
			var expected = ToNonNullImmutableList<int>(
				new WeightedEntry<int>(
					2,
					0.1f,
					(new[] { new SourceFieldLocation(0, 0, 0, 5, 0.1f) }).ToNonNullImmutableList()
				)
			);
			var actual = queryTranslator.GetMatches(
				new PreciseMatchQuerySegment("test0")
			);

			Assert.Equal<NonNullImmutableList<WeightedEntry<int>>>(
				expected,
				actual,
				new WeightedEntrySetEqualityComparer<int>(new DefaultEqualityComparer<int>())
			);
		}

		private NonNullImmutableList<WeightedEntry<TKey>> ToNonNullImmutableList<TKey>(params WeightedEntry<TKey>[] weightedEntries)
		{
			if (weightedEntries == null)
				throw new ArgumentNullException(nameof(weightedEntries));

			return weightedEntries.ToNonNullImmutableList();
		}
	}
}
