using FullTextIndexer.Common.Lists;
using FullTextIndexer.Core.Indexes;
using FullTextIndexer.Querier.QuerySegments;

namespace FullTextIndexer.Querier.QueryTranslators
{
	/// <summary>
	/// This will retrieve results from index data that match the specified IQuerySegment implementation requirements
	/// </summary>
	public interface IQueryTranslator<TKey>
	{
		/// <summary>
		/// This will never return null but may return an empty set if no matches could be made. An exception will be raised for a null querySegment
		/// reference of if the request could otherwise not be satisfied (eg. unsupported IQuerySegment implementation)
		/// </summary>
		NonNullImmutableList<WeightedEntry<TKey>> GetMatches(IQuerySegment querySegment);
	}
}
