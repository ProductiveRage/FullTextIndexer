﻿using System;
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
                throw new ArgumentOutOfRangeException("weightMultiplier");
			if (sourceLocation == null)
				throw new ArgumentNullException("sourceLocation");

            Token = token;
            WeightMultiplier = weightMultiplier;
			SourceLocation = sourceLocation;
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
		/// This will never be null
		/// </summary>
		public SourceLocation SourceLocation { get; private set; }
	}
}
