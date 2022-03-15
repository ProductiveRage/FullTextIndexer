using System;
using FullTextIndexer.Core.Indexes;

namespace FullTextIndexer.Core.TokenBreaking
{
	public class WeightAdjustingToken
    {
		public WeightAdjustingToken(string token, float weightMultiplier, SourceLocation sourceLocation)
        {
            // It could feasibly be legal for a token to contain whitespace so we'll only check for null or blank here
            if (string.IsNullOrEmpty(token))
                throw new ArgumentException("Null/blank token specified");
			if ((weightMultiplier <= 0) || (weightMultiplier > 1))
                throw new ArgumentOutOfRangeException(nameof(weightMultiplier));
            Token = token;
            WeightMultiplier = weightMultiplier;
			SourceLocation = sourceLocation ?? throw new ArgumentNullException(nameof(sourceLocation));
		}

        /// <summary>
        /// This will never be null or blank
        /// </summary>
        public string Token { get; private set; }
		
        /// <summary>
        /// This will always be greater than zero and less than or equal to one
        /// </summary>
        public float WeightMultiplier { get; private set; }

		/// <summary>
		/// This will never be null, this indicates where in the source string the particular content was located - before the Token Breaker split it up
		/// </summary>
		public SourceLocation SourceLocation { get; private set; }
	}
}
