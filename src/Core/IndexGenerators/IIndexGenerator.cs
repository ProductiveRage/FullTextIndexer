using FullTextIndexer.Common.Lists;
using FullTextIndexer.Core.Indexes;

namespace FullTextIndexer.Core.IndexGenerators
{
    public interface IIndexGenerator<TSource, TKey> where TSource : class
    {
        /// <summary>
        /// This will never return null. It will throw an exception for null input.
        /// </summary>
        IIndexData<TKey> Generate(NonNullImmutableList<TSource> data);
    }
}
