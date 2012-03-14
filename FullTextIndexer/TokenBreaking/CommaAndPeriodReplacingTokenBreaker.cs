using System;
using System.Linq;
using Common.Lists;

namespace FullTextIndexer.TokenBreaking
{
    /// <summary>
    /// This will break tokens on any whitespace character, any resulting empty entries will be ignored
    /// </summary>
    public class CommaAndPeriodReplacingTokenBreaker : ITokenBreaker
    {
        private ITokenBreaker _tokenBreaker;
        public CommaAndPeriodReplacingTokenBreaker(ITokenBreaker tokenBreaker)
        {
            if (tokenBreaker == null)
                throw new ArgumentNullException("tokenBreaker");

            _tokenBreaker = tokenBreaker;
        }

        /// <summary>
        /// This will never return null. It will throw an exception for null input.
        /// </summary>
        public NonNullOrEmptyStringList Break(string value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            return new NonNullOrEmptyStringList(
                _tokenBreaker.Break(value).Select(v => v.Replace(".", " ").Replace(",", "").Trim()).Where(v => v != "")
            );
        }
    }
}
