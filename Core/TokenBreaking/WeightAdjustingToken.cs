using System;

namespace FullTextIndexer.Core.TokenBreaking
{
    public class WeightAdjustingToken
    {
        public WeightAdjustingToken(string token, int sourceIndex, int sourceTokenLength, float weightMultiplier)
        {
            // It could feasibly be legal for a token to contain whitespace so we'll only check for null or blank here
            if (string.IsNullOrEmpty(token))
                throw new ArgumentException("Null/blank token specified");
			if (sourceIndex < 0)
				throw new ArgumentOutOfRangeException("sourceIndex", "must be zero or greater");
			if (sourceTokenLength <= 0)
				throw new ArgumentOutOfRangeException("sourceTokenLength", "must be greater than zero");
			if ((weightMultiplier <= 0) || (weightMultiplier > 1))
                throw new ArgumentOutOfRangeException("weightMultiplier");

            Token = token;
			SourceIndex = sourceIndex;
			SourceTokenLength = sourceTokenLength;
            WeightMultiplier = weightMultiplier;
        }

        /// <summary>
        /// This will never be null or blank
        /// </summary>
        public string Token { get; private set; }

		/// <summary>
		/// This is the index in the source content at which the token starts, it will always be zero or greater
		/// </summary>
		public int SourceIndex { get; private set; }
		
		/// <summary>
		/// This is the length of the token in the source content. It may not be the same as the length of the Token string since the source may have been manipulated
		/// in order to reterieve the Token value recorded here, this is the length of the content before any manipulation. This will always be greater than zero.
		/// </summary>
		public int SourceTokenLength { get; private set; }

        /// <summary>
        /// This will always be greater than zero and less than or equal to one
        /// </summary>
        public float WeightMultiplier { get; private set; }
    }
}
