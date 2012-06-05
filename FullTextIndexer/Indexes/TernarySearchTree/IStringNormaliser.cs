using System.Collections.Generic;

namespace FullTextIndexer.Indexes.TernarySearchTree
{
    public interface IStringNormaliser : IEqualityComparer<string>
    {
        /// <summary>
        /// This will never return null. If it returns an empty string then the string should not be considered elligible as a key. It will throw
        /// an exception for a null value.
        /// </summary>
        string GetNormalisedString(string value);
    }
}
