﻿using System;
using System.Linq;
using FullTextIndexer.Common.Lists;
using FullTextIndexer.Core.Indexes;

namespace FullTextIndexer.Core.TokenBreaking
{
    /// <summary>
    /// This will extend a token set by combinining strings of consecutive tokens (eg. maxNumberOfTokens is 3 and the tokens returned by the internal break are "this", "is",
    /// "a", "test" then "this", "is", "a", "test", "this is", "is a", "a test", "this is a", "is a test" will be returned). This should now be considered obsolete and searches
    /// for consecutive tokens be performed with the GetConsecutiveMatches IIndexData extension method - use of this token breaker can greatly increase the time to generate an
    /// index and it puts a cap on the length of runs of consecutive tokens that can be searched for (see the maxNumberOfTokens constructor argument). The GetConsecutiveMatches
    /// method has no such limit and can operate much more efficiently now that Source Location data is included with the match content.
    /// </summary>
    [Serializable]
	public class ConsecutiveTokenCombiningTokenBreaker : ITokenBreaker
	{
		private readonly ITokenBreaker _tokenBreaker;
		private readonly int _maxNumberOfTokens;
		private readonly WeightMultiplierDeterminer _weightMultiplierDeterminer;
		public ConsecutiveTokenCombiningTokenBreaker(
			ITokenBreaker tokenBreaker,
			int maxNumberOfTokens,
			WeightMultiplierDeterminer weightMultiplierDeterminer)
		{
			if (maxNumberOfTokens < 1)
				throw new ArgumentOutOfRangeException(nameof(maxNumberOfTokens), "must be >= 1");
			_tokenBreaker = tokenBreaker ?? throw new ArgumentNullException(nameof(tokenBreaker));
			_maxNumberOfTokens = maxNumberOfTokens;
			_weightMultiplierDeterminer = weightMultiplierDeterminer ?? throw new ArgumentNullException(nameof(weightMultiplierDeterminer));
		}

		/// <summary>
		/// For cases where multiple WeightAdjustingToken instances are combined into a new one, a new WeightMultiplier must be determined. This value is always
		/// between zero and one (exlusive lower bound, inclusive upper). This delegate will never be called with a null or empty weightMultipliersOfCombinedTokens
		/// set and must always return a value greater than zero and less than or equal to one.
		/// </summary>
		public delegate float WeightMultiplierDeterminer(ImmutableList<float> weightMultipliersOfCombinedTokens);

		/// <summary>
		/// This will never return null. It will throw an exception for null input.
		/// </summary>
		public NonNullImmutableList<WeightAdjustingToken> Break(string value)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			var initialTokens = _tokenBreaker.Break(value);
			var extendedTokens = NonNullImmutableList<WeightAdjustingToken>.Empty;
			for (var combineLength = 1; combineLength <= _maxNumberOfTokens; combineLength++)
			{
				for (var index = 0; index < initialTokens.Count - (combineLength - 1); index++)
				{
					var tokensToCombine = initialTokens.Skip(index).Take(combineLength).ToArray();
					var weightMultiplier = _weightMultiplierDeterminer(tokensToCombine.Select(t => t.WeightMultiplier).ToImmutableList());
					if ((weightMultiplier <= 0) || (weightMultiplier > 1))
						throw new Exception("Specified WeightMultiplierDeterminer return an invalid value: " + weightMultiplier);

					// The sourceTokenLength is determined by taking the end point of the last token and subtracting the start point of the first
					// token. The length couldn't be the combined lenth of each token since any breaking characters between tokens would not be
					// taken into account. The TokenIndex of the first token will be used for the new WeightAdjustingToken instance - this may
					// not be strictly accurate but since there are now overlapping tokens, it's probably the best that can be done.
					var firstToken = tokensToCombine[0];
					var lastToken = tokensToCombine[tokensToCombine.Length - 1];
					extendedTokens = extendedTokens.Add(
						new WeightAdjustingToken(
							string.Join(" ", tokensToCombine.Select(t => t.Token)),
							weightMultiplier,
							new SourceLocation(
								firstToken.SourceLocation.TokenIndex,
								firstToken.SourceLocation.SourceIndex,
								(lastToken.SourceLocation.SourceIndex + lastToken.SourceLocation.SourceTokenLength) - firstToken.SourceLocation.SourceIndex
							)
						)
					);
				}
			}
			return extendedTokens;
		}
	}
}
