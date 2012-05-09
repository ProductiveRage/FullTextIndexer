using System;
using System.Collections.Generic;
using System.Linq;
using Common.Lists;

namespace FullTextIndexer.TokenBreaking
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
        public ConsecutiveTokenCombiningTokenBreaker(ITokenBreaker tokenBreaker, int maxNumberOfTokens)
        {
            if (tokenBreaker == null)
                throw new ArgumentNullException("tokenBreaker");
            if (maxNumberOfTokens < 1)
                throw new ArgumentOutOfRangeException("maxNumberOfTokens", "must be >= 1");

            _tokenBreaker = tokenBreaker;
            _maxNumberOfTokens = maxNumberOfTokens;
        }

        /// <summary>
        /// This will never return null. It will throw an exception for null input.
        /// </summary>
        public NonNullOrEmptyStringList Break(string value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            var initialTokens = _tokenBreaker.Break(value);
            var extendedTokens = new List<string>();
            for (var combineLength = 1; combineLength <= _maxNumberOfTokens; combineLength++)
            {
                for (var index = 0; index < initialTokens.Count - (combineLength - 1); index++)
                {
                    extendedTokens.Add(
                        string.Join(" ", initialTokens.Skip(index).Take(combineLength))
                    );
                }
            }
            return new NonNullOrEmptyStringList(extendedTokens);
        }
    }
}
