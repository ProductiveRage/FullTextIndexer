using System;

namespace Querier.QuerySegments
{
	/// <summary>
	/// This indicates a query segment that data must match precisely, a much more strict string normaliser would be used that for the StandardMatchQuerySegment
	/// </summary>
	public class PreciseMatchQuerySegment : ValueQuerySegment
	{
		public PreciseMatchQuerySegment(string value) : base(value) { }
	}
}
