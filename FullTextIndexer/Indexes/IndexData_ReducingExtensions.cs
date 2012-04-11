using System;
using System.Collections.Generic;
using Common.Lists;

namespace FullTextIndexer.Indexes
{
    public static class IndexData_ReducingExtensions
    {
        /// <summary>
        /// Partially flatten an indexes keys - eg. if some keys represent matches for a particular language and some represent matches that aren't language filtered, we
        /// may wish to filter the results to a single language but excluding any keys for other languages and then combining keys that indicate the same match (where
        /// previously keys for the same match would have been distinct if they were language-specific of language-agnostic). This should only be done to generate
        /// filtered index instances, not for each search since there may be a considerable performance hit on large datasets.
        /// </summary>
        public static IndexData<TKeyNew> Reduce<TKey, TKeyNew>(
            this IndexData<TKey> index,
            Func<TKey, TKeyNew> keyTransformer,
            IEqualityComparer<TKeyNew> newKeyComparer,
            Func<ImmutableList<float>, float> weightCombiner)
        {
            if (keyTransformer == null)
                throw new ArgumentNullException("keyTransformer");
            if (newKeyComparer == null)
                throw new ArgumentNullException("newKeyComparer");

            var dataNew = new Dictionary<string, NonNullImmutableList<WeightedEntry<TKeyNew>>>(
                index.TokenComparer
            );
            foreach (var token in index.GetAllTokens())
            {
                var newMatchesForToken = new Dictionary<TKeyNew, List<float>>(newKeyComparer);
                foreach (var match in index.GetMatches(token))
                {
                    var newKey = keyTransformer(match.Key);
                    if (newKey == null)
                        throw new Exception("keyTransformer returned null value - invalid");
                    if (!newMatchesForToken.ContainsKey(newKey))
                        newMatchesForToken.Add(newKey, new List<float>());
                    newMatchesForToken[newKey].Add(match.Weight);
                }
                
                var newMatchesForTokenFlattened = new List<WeightedEntry<TKeyNew>>();
                foreach (var match in newMatchesForToken)
                {
                    var combinedWeight = weightCombiner(match.Value.ToImmutableList());
                    if (combinedWeight <= 0)
                        throw new Exception("weightCombiner returned non-positive value - invalid");
                    newMatchesForTokenFlattened.Add(new WeightedEntry<TKeyNew>(match.Key, combinedWeight));
                }
                dataNew.Add(token, newMatchesForTokenFlattened.ToNonNullImmutableList());
            }

            // Return a new instance containing the combined data
            return new IndexData<TKeyNew>(
                new ImmutableDictionary<string, NonNullImmutableList<WeightedEntry<TKeyNew>>>(
                    dataNew,
                    index.TokenComparer
                ),
                newKeyComparer
            );
        }
    }
}
