using System;
using System.Linq;
using FullTextIndexer.Common.Lists;

namespace FullTextIndexer.Core.Indexes
{
    public class WeightedEntry<TKey>
    {
		public WeightedEntry(TKey key, float weight, NonNullImmutableList<SourceFieldLocation> sourceLocations)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            if (weight <= 0)
                throw new ArgumentOutOfRangeException("weight", "must be > 0");
			if (sourceLocations == null)
				throw new ArgumentNullException("sourceLocations");
			if (!sourceLocations.Any())
				throw new ArgumentException("sourceLocations must not be empty");

            Key = key;
            Weight = weight;
			SourceLocations = sourceLocations;
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
		/// This will never be null or empty
		/// </summary>
		public NonNullImmutableList<SourceFieldLocation> SourceLocations { get; private set; }
    }
}
