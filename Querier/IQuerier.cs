using FullTextIndexer.Common.Lists;
using FullTextIndexer.Core.Indexes;

namespace FullTextIndexer.Querier
{
	public interface IQuerier<TSource, TKey>
	{
		/// <summary>
		/// This will never return null. It will throw an exception for a null or blank searchTerm.
		/// </summary>
		NonNullImmutableList<WeightedEntry<TKey>> GetMatches(string searchTerm);
	}
}
