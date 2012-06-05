using System;
using System.Collections.Generic;
using System.Linq;
using Common.Lists;
using FullTextIndexer.Indexes;

namespace FullTextIndexer.IndexGenerators
{
    public class AdditionBasedCombiningIndexGenerator<TSource, TKey> : IIndexGenerator<TSource, TKey> where TSource : class
    {
        private NonNullImmutableList<IIndexGenerator<TSource, TKey>> _indexGenerators;
        private IEqualityComparer<string> _sourceStringComparer;
        private IEqualityComparer<TKey> _dataKeyComparer;
        public AdditionBasedCombiningIndexGenerator(
            NonNullImmutableList<IIndexGenerator<TSource, TKey>> indexGenerators,
            IEqualityComparer<string> sourceStringComparer,
            IEqualityComparer<TKey> dataKeyComparer)
        {
            if (indexGenerators == null)
                throw new ArgumentNullException("indexGenerators");
            if (indexGenerators.Count == 0)
                throw new ArgumentException("Empty indexGenerators list specified - invalid");
            if (sourceStringComparer == null)
                throw new ArgumentNullException("sourceStringComparer");
            if (dataKeyComparer == null)
                throw new ArgumentNullException("dataKeyComparer");

            _indexGenerators = indexGenerators;
            _sourceStringComparer = sourceStringComparer;
            _dataKeyComparer = dataKeyComparer;
        }

        /// <summary>
        /// This will never return null. It will throw an exception for null input.
        /// </summary>
        public IIndexData<TKey> Generate(NonNullImmutableList<TSource> data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            IIndexData<TKey> combinedIndexContent = new IndexData<TKey>(
                new ImmutableDictionary<string, NonNullImmutableList<WeightedEntry<TKey>>>(
                    _sourceStringComparer
                ),
                _dataKeyComparer
            );
            foreach (var index in _indexGenerators.Select(g => g.Generate(data)))
            {
                combinedIndexContent = combinedIndexContent.Combine(
                    (new[] { index }).ToNonNullImmutableList(),
                    (x, y) => x + y
                );
            }
            return combinedIndexContent;
        }
    }
}
