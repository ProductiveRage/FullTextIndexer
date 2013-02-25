using System;
using System.Linq;
using Common.Lists;
using Querier.QuerySegments;

namespace Querier.QueryAnalysers.ContentAnalysers
{
	public static class NonNullImmutableList_IQuerySegment_Extensions
	{
		public static IQuerySegment ToSingleQuerySegment(this NonNullImmutableList<IQuerySegment> querySegments)
		{
			if (querySegments == null)
				throw new ArgumentNullException("querySegments");

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
