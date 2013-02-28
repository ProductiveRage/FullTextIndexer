using System;

namespace FullTextIndexer.Core.Indexes
{
    [Serializable]
    public class WeightedEntry<TKey>
    {
        public WeightedEntry(TKey key, float weight)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            if (weight <= 0)
                throw new ArgumentOutOfRangeException("weight", "must be > 0");

            Key = key;
            Weight = weight;
        }

        /// <summary>
        /// This will never be null
        /// </summary>
        public TKey Key { get; private set; }
        
        /// <summary>
        /// This will always be greater than zero
        /// </summary>
        public float Weight { get; private set; }
    }
}
