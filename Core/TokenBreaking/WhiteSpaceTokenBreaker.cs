using System;
using System.Collections.Generic;
using System.Linq;
using FullTextIndexer.Common.Lists;

namespace FullTextIndexer.Core.TokenBreaking
{
    /// <summary>
    /// This will break tokens on any whitespace character, any resulting empty entries will be ignored
    /// </summary>
    [Serializable]
    public class WhiteSpaceTokenBreaker : ITokenBreaker
    {
        private ITokenBreaker _optionalWrappedTokenBreaker;
        public WhiteSpaceTokenBreaker(ITokenBreaker optionalWrappedTokenBreaker)
        {
            _optionalWrappedTokenBreaker = optionalWrappedTokenBreaker;
        }
        public WhiteSpaceTokenBreaker() : this(null) { }

        /// <summary>
        /// This will never return null. It will throw an exception for null input.
        /// </summary>
        public NonNullImmutableList<WeightAdjustingToken> Break(string value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            IEnumerable<WeightAdjustingToken> tokensToBreak;
            if (_optionalWrappedTokenBreaker == null)
                tokensToBreak = new[] { new WeightAdjustingToken(value, 1) };
            else
                tokensToBreak = _optionalWrappedTokenBreaker.Break(value);
            
            var tokens = new List<WeightAdjustingToken>();
            foreach (var weightAdjustingToken in tokensToBreak)
            {
                // Passing (char[]) null will cause breaking on any whitespace char
                tokens.AddRange(
                    weightAdjustingToken.Token
                        .Split((char[])null, StringSplitOptions.RemoveEmptyEntries)
                        .Select(token => new WeightAdjustingToken(token, weightAdjustingToken.WeightMultiplier))
                );
            };
            return tokens.ToNonNullImmutableList();
        }
    }
}
