using System;
using System.Linq;
using System.Text.RegularExpressions;
using Common.Lists;

namespace FullTextIndexer.TokenBreaking
{
    /// <summary>
    /// This will remove all punctuation. This may cause problems if there are no spaces around "," or "." characters as the words either side will be combined.
    /// </summary>
    public class PunctuationRemovingTokenBreaker : ITokenBreaker
    {
        private static Regex PunctuationRemover = new Regex("\\p{P}+", RegexOptions.Compiled);
        
        private ITokenBreaker _tokenBreaker;
        public PunctuationRemovingTokenBreaker(ITokenBreaker tokenBreaker)
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
                _tokenBreaker.Break(value).Select(v => PunctuationRemover.Replace(v, "").Trim()).Where(v => v != "")
            );
        }
    }
}
