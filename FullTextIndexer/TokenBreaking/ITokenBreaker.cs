using Common.Lists;

namespace FullTextIndexer.TokenBreaking
{
    public interface ITokenBreaker
    {
        /// <summary>
        /// This will never return null. It will throw an exception for null input.
        /// </summary>
        NonNullOrEmptyStringList Break(string value);
    }
}
