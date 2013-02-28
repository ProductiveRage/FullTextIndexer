using System;
using System.Collections.Generic;
using FullTextIndexer.Common.Lists;

namespace FullTextIndexer.Core.Indexes
{
    public interface IIndexData<TKey>
    {
        /// <summary>
        /// This will throw an exception for null or blank input. It will never return null. If there are no matches then an empty list will be returned.
        /// </summary>
        NonNullImmutableList<WeightedEntry<TKey>> GetMatches(string source);

        /// <summary>
        /// This will return a new instance that combines the source instance's data with the data other IndexData instances using the specified weight combiner. In
        /// a case where there are different TokenComparer implementations on this instance and on any of the indexesToAdd, the comparer from the current instance
        /// will be used. It is recommended that a consistent TokenComparer be used at all times. An exception will be thrown for null dataToAdd or weightCombiner
        /// references.
        /// </summary>
        IIndexData<TKey> Combine(NonNullImmutableList<IIndexData<TKey>> indexesToAdd, IndexGenerators.IndexGenerator.WeightedEntryCombiner weightCombiner);

        /// <summary>
        /// This will return a new instance without any WeightedEntry values whose Keys match the removeIf predicate. If tokens are left without any WeightedEntry
        /// values then the token will be excluded from the new data. This will never return null. It will throw an exception for a null removeIf.
        /// </summary>
        IIndexData<TKey> Remove(Predicate<TKey> removeIf);

        /// <summary>
        /// This will never return null, the returned dictionary will have this instance's KeyNormaliser as its comparer
        /// </summary>
        IDictionary<string, NonNullImmutableList<WeightedEntry<TKey>>> ToDictionary();

        /// <summary>
        /// This will never return null
        /// </summary>
        NonNullOrEmptyStringList GetAllTokens();

        /// <summary>
        /// This will never return null
        /// </summary>
        IEqualityComparer<string> TokenComparer { get; }

        /// <summary>
        /// This will never return null
        /// </summary>
        IEqualityComparer<TKey> KeyComparer { get; }
    }
}
