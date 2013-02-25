using System;
using Querier.QueryAnalysers.StringNavigators;
using Querier.QuerySegments;

namespace Querier.QueryAnalysers.ContentAnalysers
{
	public class ProcessedQuerySegment
	{
		public ProcessedQuerySegment(IWalkThroughStrings stringNavigator, IQuerySegment querySegment)
		{
			if (stringNavigator == null)
				throw new ArgumentNullException("stringNavigator");
			if (querySegment == null)
				throw new ArgumentNullException("querySegment");

			StringNavigator = stringNavigator;
			QuerySegment = querySegment;
		}
		
		/// <summary>
		/// This will never be null
		/// </summary>
		public IWalkThroughStrings StringNavigator { get; private set; }

		/// <summary>
		/// This will never be null
		/// </summary>
		public IQuerySegment QuerySegment { get; private set; }
	}
}
