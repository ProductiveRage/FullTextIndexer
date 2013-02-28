using System;

namespace FullTextIndexer.Querier.QuerySegments
{
	/// <summary>
	/// For a set of query segments, the default behaviour is that data is considered to be identified by the query if it satisfies the requirements of any of the segments.
	/// If any of the segments is of this type, then data MUST be identified by the wrapped query segment.
	/// </summary>
	public class CompulsoryQuerySegment : IQuerySegment
	{
		public CompulsoryQuerySegment(IQuerySegment segment)
		{
			if (segment == null)
				throw new ArgumentNullException("segment");

			Segment = segment;
		}

		/// <summary>
		/// This will never be null
		/// </summary>
		public IQuerySegment Segment { get; private set; }
	}
}
