using System;
using FullTextIndexer.Common.Lists;

namespace FullTextIndexer.Querier.QuerySegments
{
	/// <summary>
	/// The default behaviour for a set of query segments is that data may be considered for the results if any of the query segments identify it, however this
	/// behaviour may be altered if the segments are of the type CompulsoryQuerySegment or ExcludingQuerySegment
	/// </summary>
	public class CombiningQuerySegment : IQuerySegment
	{
		public CombiningQuerySegment(NonNullImmutableList<IQuerySegment> segments)
		{
			if (segments == null)
				throw new ArgumentNullException(nameof(segments));
			if (segments.Count == 0)
				throw new ArgumentException("There must be at least one segment specified");

			Segments = segments;
		}

		/// <summary>
		/// This will never be null or empty
		/// </summary>
		public NonNullImmutableList<IQuerySegment> Segments { get; private set; }
	}
}
