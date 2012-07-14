using System;
using System.Collections.Generic;
using System.Linq;
using Common.Lists;

namespace FullTextIndexer.TokenBreaking
{
    /// <summary>
    /// This will break tokens on any whitespace character, any resulting empty entries will be ignored
    /// </summary>
    [Serializable]
    public class WhiteSpaceTokenBreaker : ITokenBreaker
    {
        private ITokenBreaker _tokenBreaker;
        public WhiteSpaceTokenBreaker(ITokenBreaker tokenBreaker)
        {
            if (tokenBreaker == null)
                throw new ArgumentNullException("tokenBreaker");

            _tokenBreaker = tokenBreaker;
        }

        /// <summary>
        /// This will never return null. It will throw an exception for null input.
        /// </summary>
        public NonNullImmutableList<WeightAdjustingToken> Break(string value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            var tokens = new List<WeightAdjustingToken>();
            foreach (var weightAdjustingToken in _tokenBreaker.Break(value))
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
