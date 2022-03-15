using System;
using FullTextIndexer.Common.Lists;

namespace FullTextIndexer.Core.TokenBreaking
{
    /// <summary>
    /// This will replace specified characters with whitespace. This can be useful for parsing content with code examples which separate keywords and variable names
    /// with various brackets or other operators. It may also be useful if there is text content without consistently including spaces after commas or full stops.
    /// The expected tokenBreaker specified in the constructor would be the WhiteSpaceTokenBreaker since this effectively works as the step before that.
    /// </summary>
    [Serializable]
	public class WhiteSpaceExtendingTokenBreaker : ITokenBreaker
    {
        private readonly ImmutableList<char> _charsToTreatAsWhitespace;
        private readonly ITokenBreaker _tokenBreaker;
        public WhiteSpaceExtendingTokenBreaker(ImmutableList<char> charsToTreatAsWhitespace, ITokenBreaker tokenBreaker)
        {
            _charsToTreatAsWhitespace = charsToTreatAsWhitespace ?? throw new ArgumentNullException(nameof(charsToTreatAsWhitespace));
            _tokenBreaker = tokenBreaker ?? throw new ArgumentNullException(nameof(tokenBreaker));
        }

        /// <summary>
        /// This will never return null. It will throw an exception for null input.
        /// </summary>
        public NonNullImmutableList<WeightAdjustingToken> Break(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            foreach (var charToReplace in _charsToTreatAsWhitespace)
                value = value.Replace(charToReplace, ' ');
                
            return _tokenBreaker.Break(value);
        }
    }
}
