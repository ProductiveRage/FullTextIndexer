using Querier.QuerySegments;

namespace Querier.QueryAnalysers
{
	public interface IAnalyseQueries
	{
		/// <summary>
		/// This will never return null, it will throw an exception for a null or blank search term
		/// </summary>
		IQuerySegment Analyse(string search);
	}
}
