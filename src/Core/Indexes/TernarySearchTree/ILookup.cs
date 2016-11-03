namespace FullTextIndexer.Core.Indexes.TernarySearchTree
{
    public interface ILookup<TKey, TValue>
    {
        /// <summary>
        /// This will throw an exception for a key not present in the data
        /// </summary>
        TValue this[TKey key] { get; }

        /// <summary>
        /// This will return true if the specified key was found and will set the value output parameter to the corresponding value. If it return false then the
        /// value output parameter should not be considered to be defined.
        /// </summary>
        bool TryGetValue(TKey key, out TValue value);
    }
}
