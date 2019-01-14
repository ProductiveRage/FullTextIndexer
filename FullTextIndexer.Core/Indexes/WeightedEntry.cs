using System;
using FullTextIndexer.Common.Lists;

namespace FullTextIndexer.Core.Indexes
{
#if NET45
	[Serializable]
#endif
	public class WeightedEntry<TKey>
	{
		public WeightedEntry(TKey key, float weight, NonNullImmutableList<SourceFieldLocation> sourceLocationsIfRecorded)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			if (weight <= 0)
				throw new ArgumentOutOfRangeException("weight", "must be > 0");
			if ((sourceLocationsIfRecorded != null) && !sourceLocationsIfRecorded.Any())
				throw new ArgumentException("sourceLocationsIfRecorded must not be empty if it is non-null");

			Key = key;
			Weight = weight;
			SourceLocationsIfRecorded = sourceLocationsIfRecorded;
		}

		/// <summary>
		/// This will never be null
		/// </summary>
		public TKey Key { get; private set; }
		
		/// <summary>
		/// This will always be greater than zero
		/// </summary>
		public float Weight { get; private set; }

		/// <summary>
		/// This will be null if the source location data is not recorded by the index generator but it will never be an empty list if it is not null
		/// </summary>
		public NonNullImmutableList<SourceFieldLocation> SourceLocationsIfRecorded { get; private set; }
	}
}
