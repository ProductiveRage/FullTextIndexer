using System;
using FullTextIndexer.Common.Lists;

namespace FullTextIndexer.Core.Indexes
{
	/// <summary>
	/// This extends the WeightedEntry class with the search term, it won't be recorded in the Index both in the interests of space and because the index only knows what
	/// tokens COULD match to it, the actual searchTerm value here should be that which was queried for. This may be of particular interest where a multi-word query has
	/// been performed using the GetPartialMatches extension method.
	/// </summary>
    [Serializable]
    public class WeightedEntryWithTerm<TKey> : WeightedEntry<TKey>
    {
		// We can use rely on covariance allowing the sourceLocations to be used to generate a NonNullImmutableList<SourceFieldLocation> using the constructor with the
		// IEnumerable argument, but NonNullImmutableList doesn't support covariance so we can't pass the sourceLocations references straight to the base constructor
		public WeightedEntryWithTerm(TKey key, float weight, NonNullImmutableList<SourceFieldLocationWithTerm> sourceLocations)
			: base(key, weight, new NonNullImmutableList<SourceFieldLocation>(sourceLocations))
		{
			SourceLocations = sourceLocations;
		}

		/// <summary>
		/// This will never be null or empty
		/// </summary>
		public new NonNullImmutableList<SourceFieldLocationWithTerm> SourceLocations { get; private set; }
    }
}
