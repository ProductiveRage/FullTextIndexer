using System;
using System.Collections.Generic;
using System.Linq;
using FullTextIndexer.Common.Lists;

namespace FullTextIndexer.Core.TokenBreaking
{
    /// <summary>
    /// This will extend a token set by combinining strings of consecutive tokens (eg. maxNumberOfTokens is 3 and the tokens returned by the internal break are "this", "is",
    /// "a", "test" then "this", "is", "a", "test", "this is", "is a", "a test", "this is a", "is a test" will be returned)
    /// </summary>
    [Serializable]
    public class ConsecutiveTokenCombiningTokenBreaker : ITokenBreaker
    {
        private ITokenBreaker _tokenBreaker;
        private int _maxNumberOfTokens;
        private WeightMultiplierDeterminer _weightMultiplierDeterminer;
        public ConsecutiveTokenCombiningTokenBreaker(
            ITokenBreaker tokenBreaker,
            int maxNumberOfTokens,
            WeightMultiplierDeterminer weightMultiplierDeterminer)
        {
            if (tokenBreaker == null)
                throw new ArgumentNullException("tokenBreaker");
            if (maxNumberOfTokens < 1)
                throw new ArgumentOutOfRangeException("maxNumberOfTokens", "must be >= 1");
            if (weightMultiplierDeterminer == null)
                throw new ArgumentNullException("weightMultiplierDeterminer");

            _tokenBreaker = tokenBreaker;
            _maxNumberOfTokens = maxNumberOfTokens;
            _weightMultiplierDeterminer = weightMultiplierDeterminer;
        }

        /// <summary>
        /// When tokens are combined this may affect the WeightMultiplier of the generated WeightAdjustingToken, a delegate of this form will be called to specify the
        /// multiplier. The numberOfTokens will always be one or greater, it must always return a value greater than zero. If the weight of matches should not be
        /// affected by the number of tokens that are combined then this would always return one.
        /// </summary>
        public delegate float WeightMultiplierDeterminer(int numberOfTokensCombined);

        /// <summary>
        /// This will never return null. It will throw an exception for null input.
        /// </summary>
        public NonNullImmutableList<WeightAdjustingToken> Break(string value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            var initialTokens = _tokenBreaker.Break(value);
            var extendedTokens = new List<WeightAdjustingToken>();
            for (var combineLength = 1; combineLength <= _maxNumberOfTokens; combineLength++)
            {
                var weightMultiplier = _weightMultiplierDeterminer(combineLength);
                if (weightMultiplier <= 0)
                    throw new Exception("Specified weightMultiplier return invalid value (" + weightMultiplier + ") to numberOfTokensCombined: " + combineLength);
                for (var index = 0; index < initialTokens.Count - (combineLength - 1); index++)
                {
                    extendedTokens.Add(
                        new WeightAdjustingToken(
                            string.Join(" ", initialTokens.Skip(index).Take(combineLength)),
                            weightMultiplier
                        )
                    );
                }
            }
            return extendedTokens.ToNonNullImmutableList();
        }
    }
}
