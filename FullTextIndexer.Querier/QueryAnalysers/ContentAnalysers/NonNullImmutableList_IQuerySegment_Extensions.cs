using System;
using System.Linq;
using FullTextIndexer.Common.Lists;
using FullTextIndexer.Querier.QuerySegments;

namespace FullTextIndexer.Querier.QueryAnalysers.ContentAnalysers
{
	public static class NonNullImmutableList_IQuerySegment_Extensions
	{
		public static IQuerySegment ToSingleQuerySegment(this NonNullImmutableList<IQuerySegment> querySegments)
		{
			if (querySegments == null)
				throw new ArgumentNullException(nameof(querySegments));

			var significantQuerySegments = querySegments.Where(s => !(s is NoMatchContentQuerySegment));
			if (!significantQuerySegments.Any())
				return new NoMatchContentQuerySegment();
			else if (significantQuerySegments.Count() == 1)
				return significantQuerySegments.First();
			else
			{
				return new CombiningQuerySegment(
					significantQuerySegments.ToNonNullImmutableList()
				);
			}
		}
	}
}
