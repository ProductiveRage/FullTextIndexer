using System;

namespace FullTextIndexer.Querier.QuerySegments
{
	/// <summary>
	/// This indicates that results that match the wrapped segment should not be included in the final results
	/// </summary>
	public class ExcludingQuerySegment : IQuerySegment
	{
		public ExcludingQuerySegment(IQuerySegment segment)
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
