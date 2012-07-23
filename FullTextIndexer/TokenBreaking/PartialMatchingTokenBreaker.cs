using System;
using System.Collections.Generic;
using Common.Lists;

namespace FullTextIndexer.TokenBreaking
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
        private int _minLengthOfPartialMatches, _maxLengthOfPartialMatches;
        private ITokenBreaker _tokenBreaker, _optionalPrePartialMatchTokenBreaker;
        private PartialMatchWeightDeterminer _partialMatchWeightDeterminer;
        public PartialMatchingTokenBreaker(
            int minLengthOfPartialMatches,
            int maxLengthOfPartialMatches,
            ITokenBreaker tokenBreaker,
            ITokenBreaker optionalPrePartialMatchTokenBreaker,
            PartialMatchWeightDeterminer partialMatchWeightDeterminer)
        {
            if (minLengthOfPartialMatches <= 0)
                throw new ArgumentOutOfRangeException("minLengthOfPartialMatches", "must be greater than zero");
            if (maxLengthOfPartialMatches <= 0)
                throw new ArgumentOutOfRangeException("maxLengthOfPartialMatches", "must be greater than zero");
            if (maxLengthOfPartialMatches < minLengthOfPartialMatches)
                throw new ArgumentOutOfRangeException("maxLengthOfPartialMatches", "must be greater than minLengthOfPartialMatches");
            if (tokenBreaker == null)
                throw new ArgumentNullException("tokenBreaker");
            if (partialMatchWeightDeterminer == null)
                throw new ArgumentNullException("partialMatchWeightDeterminer");
            
            _minLengthOfPartialMatches = minLengthOfPartialMatches;
            _maxLengthOfPartialMatches = maxLengthOfPartialMatches;
            _tokenBreaker = tokenBreaker;
            _optionalPrePartialMatchTokenBreaker = optionalPrePartialMatchTokenBreaker;
            _partialMatchWeightDeterminer = partialMatchWeightDeterminer;
        }

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
                throw new ArgumentNullException("value");

            var tokens = new List<WeightAdjustingToken>();
            foreach (var weightAdjustingToken in _tokenBreaker.Break(value))
            {
                // Add the unaltered token to the list
                tokens.Add(weightAdjustingToken);

                // Generate partial match data for this token
                foreach (var weightAdjustingSubToken in GetTokensForPartialMatchGeneration(weightAdjustingToken.Token))
                {
                    foreach (var subTokenMatchVariation in GenerateAllMatchVariations(weightAdjustingSubToken.Token))
                    {
                        // If this current variation is the unaltered token value returned by the core tokenBreaker then ignore it as it's already
                        // been added to the list
                        if (subTokenMatchVariation == weightAdjustingToken.Token)
                            continue;

                        // Get the weight adjustment for the match variation; exclude it if zero or combine it with the weightAdjustingSubToken's 
                        // WeightMultiplier if greater than zero (less than zero is invalid and will cause an exception to be thrown)
                        var partialMatchWeightMultiplier = _partialMatchWeightDeterminer(weightAdjustingToken.Token, subTokenMatchVariation);
                        if (partialMatchWeightMultiplier < 0)
                            throw new Exception("partialMatchWeightMultiplier returned negative value");
                        else if (partialMatchWeightMultiplier == 0)
                            continue;

                        tokens.Add(new WeightAdjustingToken(
                            subTokenMatchVariation,
                            weightAdjustingToken.WeightMultiplier * weightAdjustingSubToken.WeightMultiplier * partialMatchWeightMultiplier
                        ));
                    }
                }
            };
            return tokens.ToNonNullImmutableList();
        }

        /// <summary>
        /// Further break any broken token using the optionalPrePartialMatchTokenBreaker, if specified (if not then return a set containing only the
        /// specified token with a weight adjustment of one, meaning no adjustment required)
        /// </summary>
        private IEnumerable<WeightAdjustingToken> GetTokensForPartialMatchGeneration(string token)
        {
            if (token == null)
                throw new ArgumentNullException("token");

            if (_optionalPrePartialMatchTokenBreaker == null)
                return new[] { new WeightAdjustingToken(token, 1) };

            return _optionalPrePartialMatchTokenBreaker.Break(token);
        }

        /// <summary>
        /// This will generate all match variations for a specified value, taking into account the minLengthOfPartialMatche and maxLengthOfPartialMatches
        /// constraints. Note: The original value will be included in the returned set as one of the variations. This will never return null nor a set
        /// that contains any nulls.
        /// </summary>
        private IEnumerable<string> GenerateAllMatchVariations(string value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            if (value.Length < _minLengthOfPartialMatches)
                return new string[0];

            var partialMatches = new List<string>();
            for (var index = 0; index < value.Length; index++)
            {
                for (var length = _minLengthOfPartialMatches; length <= Math.Min(value.Length - index, _maxLengthOfPartialMatches); length++)
                    partialMatches.Add(value.Substring(index, length));
            }
            return partialMatches;
        }
    }
}
