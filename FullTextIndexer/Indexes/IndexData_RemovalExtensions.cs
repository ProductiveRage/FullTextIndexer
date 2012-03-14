using System;
using System.Collections.Generic;
using System.Linq;
using Common.Lists;

namespace FullTextIndexer.Indexes
{
    public static class IndexData_RemovalExtensions
    {
        /// <summary>
        /// This will return a new IndexData instance without any data relating to keys identified by the removeIf filter
        /// </summary>
        public static IndexData<TKey> Remove<TKey>(this IndexData<TKey> index, Predicate<TKey> removeIf)
        {
            if (removeIf == null)
                throw new ArgumentNullException("removeIf");

            var dataNew = new Dictionary<string, NonNullImmutableList<WeightedEntry<TKey>>>(
                index.TokenComparer
            );
            foreach (var token in index.GetAllTokens())
            {
                var matchesForToken = index.GetMatches(token);
                var trimmedWeightedEntries = matchesForToken.Where(m => !removeIf(m.Key));
                if (trimmedWeightedEntries.Any())
                    dataNew.Add(token, trimmedWeightedEntries.ToNonNullImmutableList());
            }

            // Return a new instance containing the combined data
            return new IndexData<TKey>(
                new ImmutableDictionary<string, NonNullImmutableList<WeightedEntry<TKey>>>(
                    dataNew,
                    index.TokenComparer
                ),
                index.KeyComparer
            );
        }
    }
}
