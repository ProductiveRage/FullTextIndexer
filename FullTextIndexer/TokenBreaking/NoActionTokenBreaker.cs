using System;
using Common.Lists;

namespace FullTextIndexer.TokenBreaking
{
    [Serializable]
    public class NoActionTokenBreaker : ITokenBreaker
    {
        /// <summary>
        /// This will never return null. It will throw an exception for null input.
        /// </summary>
        public NonNullOrEmptyStringList Break(string value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            return new NonNullOrEmptyStringList(new[] { value });
        }
    }
}
