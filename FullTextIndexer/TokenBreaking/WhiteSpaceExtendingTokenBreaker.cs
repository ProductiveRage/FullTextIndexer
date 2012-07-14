using System;
using Common.Lists;

namespace FullTextIndexer.TokenBreaking
{
    /// <summary>
    /// This will replace specified characters with whitespace. This can be useful for parsing content with code examples which separate keywords and variable names
    /// with various brackets or other operators. It may also be useful if there is text content without consistently including spaces after commas or full stops.
    /// The expected tokenBreaker specified in the constructor would be the WhiteSpaceTokenBreaker since this effectively works as the step before that.
    /// </summary>
    [Serializable]
    public class WhiteSpaceExtendingTokenBreaker : ITokenBreaker
    {
        private ImmutableList<char> _charsToTreatAsWhitespace;
        private ITokenBreaker _tokenBreaker;
        public WhiteSpaceExtendingTokenBreaker(ImmutableList<char> charsToTreatAsWhitespace, ITokenBreaker tokenBreaker)
        {
            if (charsToTreatAsWhitespace == null)
                throw new ArgumentNullException("charsToTreatAsWhitespace");
            if (tokenBreaker == null)
                throw new ArgumentNullException("tokenBreaker");

            _charsToTreatAsWhitespace = charsToTreatAsWhitespace;
            _tokenBreaker = tokenBreaker;
        }

        /// <summary>
        /// This will never return null. It will throw an exception for null input.
        /// </summary>
        public NonNullImmutableList<WeightAdjustingToken> Break(string value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            foreach (var charToReplace in _charsToTreatAsWhitespace)
                value = value.Replace(charToReplace, ' ');
                
            return _tokenBreaker.Break(value);
        }
    }
}
