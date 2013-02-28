using FullTextIndexer.Common.Lists;

namespace FullTextIndexer.Core.TokenBreaking
{
    /// <summary>
    /// When a content is broken up into individual tokens it may be desirable to assign different weights to tokens based upon the manner in which the tokens are extracted.
    /// For example, a given word in an input string may be extracted whole and then extracted again in smaller parts that are still recognised as valid words in order to
    /// create a form of partial matching; the first token would keep a weight of 1 while the partial matches may have fractional values to indicate less precise matches.
    /// </summary>
    public interface ITokenBreaker
    {
        /// <summary>
        /// This will never return null. It will throw an exception for null input.
        /// </summary>
        NonNullImmutableList<WeightAdjustingToken> Break(string value);
    }
}
