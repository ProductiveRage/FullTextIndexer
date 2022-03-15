using System;
using System.Collections.Generic;
using System.Linq;
using FullTextIndexer.Common.Lists;
using FullTextIndexer.Core.Indexes;

namespace FullTextIndexer.Core.TokenBreaking
{
    /// <summary>
    /// Extract tokens from an input string using a specified Token Breaker and then generate partial match tokens for each entry by taking substrings
    /// from each token. The partialMatchWeightDeterminer may be used to specify a weight adjustment for partial matches or to exclude particular
    /// partial matches by reporting a zero weight for them. The optionalPrePartialMatchTokenBreaker may apply additional processing before additional
    /// tokens are generated - eg. a Token Breaker which splits on whitespace may be specified as the tokenBreaker and will split the value "this string
    /// is all lower-cased" into five tokens while an optionalPrePartialMatchTokenBreaker that breaks on punctuation would break "lower-cased" into
    /// "lower" and "cased" before partial matches are generated for it; potentially returning "lower", "cased", "low", "owe", "ower" and "case" if a
    /// partial match weight determiner was used that returned zero for any non-English words.
    /// </summary>
    [Serializable]
	public class PartialMatchingTokenBreaker : ITokenBreaker
	{
		private readonly int _minLengthOfPartialMatches, _maxLengthOfPartialMatches;
		private readonly bool _fromStartOfTokenOnly;
		private readonly ITokenBreaker _tokenBreaker, _optionalPrePartialMatchTokenBreaker;
		private readonly PartialMatchWeightDeterminer _partialMatchWeightDeterminer;
		public PartialMatchingTokenBreaker(
			int minLengthOfPartialMatches,
			int maxLengthOfPartialMatches,
			bool fromStartOfTokenOnly,
			ITokenBreaker tokenBreaker,
			ITokenBreaker optionalPrePartialMatchTokenBreaker,
			PartialMatchWeightDeterminer partialMatchWeightDeterminer)
		{
			if (minLengthOfPartialMatches <= 0)
				throw new ArgumentOutOfRangeException(nameof(minLengthOfPartialMatches), "must be greater than zero");
			if (maxLengthOfPartialMatches <= 0)
				throw new ArgumentOutOfRangeException(nameof(maxLengthOfPartialMatches), "must be greater than zero");
			if (maxLengthOfPartialMatches < minLengthOfPartialMatches)
				throw new ArgumentOutOfRangeException(nameof(maxLengthOfPartialMatches), "must be greater than minLengthOfPartialMatches");
			_minLengthOfPartialMatches = minLengthOfPartialMatches;
			_maxLengthOfPartialMatches = maxLengthOfPartialMatches;
			_fromStartOfTokenOnly = fromStartOfTokenOnly;
			_tokenBreaker = tokenBreaker ?? throw new ArgumentNullException(nameof(tokenBreaker));
			_optionalPrePartialMatchTokenBreaker = optionalPrePartialMatchTokenBreaker;
			_partialMatchWeightDeterminer = partialMatchWeightDeterminer ?? throw new ArgumentNullException(nameof(partialMatchWeightDeterminer));
		}

		public PartialMatchingTokenBreaker(
			int minLengthOfPartialMatches,
			int maxLengthOfPartialMatches,
			bool fromStartOfTokenOnly,
			ITokenBreaker tokenBreaker,
			PartialMatchWeightDeterminer partialMatchWeightDeterminer) : this(minLengthOfPartialMatches, maxLengthOfPartialMatches, fromStartOfTokenOnly, tokenBreaker, null, partialMatchWeightDeterminer) { }

		public PartialMatchingTokenBreaker(
			int minLengthOfPartialMatches,
			int maxLengthOfPartialMatches,
			ITokenBreaker tokenBreaker,
			PartialMatchWeightDeterminer partialMatchWeightDeterminer) : this(minLengthOfPartialMatches, maxLengthOfPartialMatches, false, tokenBreaker, partialMatchWeightDeterminer) { }

		/// <summary>
		/// This will return the weight multiplier that a generated partial match should be assigned - the fragment value will be a partial match
		/// variation generated from the specified token. This must never return a value less than zero. Returning a value of zero means that the
		/// fragment should not be included in the data.
		/// </summary>
		public delegate float PartialMatchWeightDeterminer(string token, string fragment);

		/// <summary>
		/// This will never return null. It will throw an exception for null input.
		/// </summary>
		public NonNullImmutableList<WeightAdjustingToken> Break(string value)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			var tokens = new List<WeightAdjustingToken>();
			foreach (var weightAdjustingToken in _tokenBreaker.Break(value))
			{
				// Add the unaltered token to the list
				tokens.Add(weightAdjustingToken);

				// Generate partial match data for this token
				foreach (var weightAdjustingSubToken in GetTokensForPartialMatchGeneration(weightAdjustingToken))
				{
					foreach (var subTokenMatchVariation in GenerateAllMatchVariations(weightAdjustingSubToken))
					{
						// If this current variation is the unaltered token value returned by the core tokenBreaker then ignore it as it's already
						// been added to the list
						if (subTokenMatchVariation.Token == weightAdjustingToken.Token)
							continue;

						// Get the weight adjustment for the match variation; exclude it if zero or combine it with the weightAdjustingSubToken's 
						// WeightMultiplier if greater than zero (less than zero is invalid and will cause an exception to be thrown)
						var partialMatchWeightMultiplier = _partialMatchWeightDeterminer(weightAdjustingToken.Token, subTokenMatchVariation.Token);
						if (partialMatchWeightMultiplier < 0)
							throw new Exception("partialMatchWeightMultiplier returned negative value");
						else if (partialMatchWeightMultiplier == 0)
							continue;

						tokens.Add(new WeightAdjustingToken(
							subTokenMatchVariation.Token,
							weightAdjustingToken.WeightMultiplier * weightAdjustingSubToken.WeightMultiplier * partialMatchWeightMultiplier,
							new SourceLocation(
								subTokenMatchVariation.SourceLocation.TokenIndex,
								subTokenMatchVariation.SourceLocation.SourceIndex,
								subTokenMatchVariation.SourceLocation.SourceTokenLength
							)
						));
					}
				}
			};
			return tokens.ToNonNullImmutableList();
		}

		/// <summary>
		/// Further break any broken token using the optionalPrePartialMatchTokenBreaker, if specified (if not then return a set containing only the
		/// specified token)
		/// </summary>
		private IEnumerable<WeightAdjustingToken> GetTokensForPartialMatchGeneration(WeightAdjustingToken token)
		{
			if (token == null)
				throw new ArgumentNullException(nameof(token));

			if (_optionalPrePartialMatchTokenBreaker == null)
				return new[] { token };

			// The SourceLocation values do not have to be altered; they are used to indicate which word (or segment) in the source content is being matched,
			// if matching that word partially then we still want that word to be indicated as being matched, even though only a section of that word will
			// actually be being matched.
			return _optionalPrePartialMatchTokenBreaker.Break(token.Token)
				.Select(t => new WeightAdjustingToken(
					t.Token,
					token.WeightMultiplier,
					token.SourceLocation
				));
		}

		/// <summary>
		/// This will generate all match variations for a specified value, taking into account the minLengthOfPartialMatche and maxLengthOfPartialMatches
		/// constraints. Note: The original value will be included in the returned set as one of the variations. This will never return null nor a set
		/// that contains any nulls.
		/// </summary>
		private IEnumerable<WeightAdjustingToken> GenerateAllMatchVariations(WeightAdjustingToken token)
		{
			if (token == null)
				throw new ArgumentNullException(nameof(token));

			if (token.Token.Length < _minLengthOfPartialMatches)
				return Array.Empty<WeightAdjustingToken>();

			var partialMatches = new List<WeightAdjustingToken>();
			for (var index = 0; index < token.Token.Length; index++)
			{
				for (var length = _minLengthOfPartialMatches; length <= Math.Min(token.Token.Length - index, _maxLengthOfPartialMatches); length++)
				{
					// 2019-03-13 Dion: Because this partial matching may split a string in the middle of a sequence of UTF-16 code units that make up a single
					// Unicode code point (e.g. emoji require multiple UTf-16 code units to make up the code point), we discard any strings that form broken
					// Unicode code points here. This is done by checking if the end of the string is a "lead surrogate" (aka "high surrogate").
					// Not doing this results in problems later on when string.Normalize() is called on these tokens and finds corrupt Unicode text.
					var partialString = token.Token.Substring(index, length);
					if (char.IsHighSurrogate(partialString, partialString.Length - 1))
						continue;

					// The token's SourceLocation is being maintained for the same reason as they are in GetTokensForPartialMatchGeneration
					partialMatches.Add(new WeightAdjustingToken(
						partialString,
						token.WeightMultiplier,
						token.SourceLocation
					));
				}

				// If we only want to extract sub tokens from the start of the string then we only need a single pass of the outer loop
				if (_fromStartOfTokenOnly)
					break;
			}
			return partialMatches;
		}
	}
}
