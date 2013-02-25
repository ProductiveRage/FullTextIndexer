using System;

namespace Querier.QuerySegments
{
	/// <summary>
	/// This indicates a query segment value that a more forgiving string normaliser would be used with (unlike the PreciseMatchQuerySegment which indicates a value that must
	/// be found precisely as specified)
	/// </summary>
	public class StandardMatchQuerySegment : ValueQuerySegment
	{
		public StandardMatchQuerySegment(string value) : base(value) { }
	}
}
