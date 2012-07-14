using System;

namespace FullTextIndexer.TokenBreaking
{
    public class WeightAdjustingToken
    {
        public WeightAdjustingToken(string token, float weightMultiplier)
        {
            // It could feasibly be legal for a token to contain whitespace so we'll only check for null or blank here
            if (string.IsNullOrEmpty(token))
                throw new ArgumentException("Null/blank token specified");
            if ((weightMultiplier <= 0) || (weightMultiplier > 1))
                throw new ArgumentOutOfRangeException("weightMultiplier");
        }

        /// <summary>
        /// This will never be null or blank
        /// </summary>
        public string Token { get; private set; }

        /// <summary>
        /// This will always be greater than zero and less than or equal to one
        /// </summary>
        public float WeightMultiplier { get; private set; }
    }
}
