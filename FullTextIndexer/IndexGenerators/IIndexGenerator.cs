using Common.Lists;

namespace FullTextIndexer.IndexGenerators
{
    public interface IIndexGenerator<TSource, TKey> where TSource : class
    {
        /// <summary>
        /// This will never return null. It will throw an exception for null input.
        /// </summary>
        IndexData<TKey> Generate(NonNullImmutableList<TSource> data);
    }
}
