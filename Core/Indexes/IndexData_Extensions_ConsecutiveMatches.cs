using System;
using System.Collections.Generic;
using System.Linq;
using FullTextIndexer.Common.Lists;
using FullTextIndexer.Core.IndexGenerators;
using FullTextIndexer.Core.TokenBreaking;

namespace FullTextIndexer.Core.Indexes
{
    public static class IndexData_Extensions_ConsecutiveMatches
    {
		/// <summary>
		/// This will break down a source search term into words (according to the logic of the specified token breaker) and then return matches where the words were found in a run in a
		/// content section. Unlike GetPartialMatches it is not possible for an entry to be considered a match because it contains all of the terms in its content, the terms must be
		/// present in one content field, together, in the order in which they are present in the search term. This allows for similar behaviour to that intended for the
		/// ConsecutiveTokenCombiningTokenBreaker, but this offers greater performance (constructing a TernarySearchTreeDictionary to back an IndexData instance can be expensive on
		/// processing time to generate, and disk / memory space to store, the runs of tokens). This also has the benefit that there is no cap on the number of tokens that can be
		/// matched consecutively (a limit on this had to be decided at index generation time when using the ConsecutiveTokenCombiningTokenBreaker). There are two sets of weight
		/// combining calculations required; the first (handled by the weightCombinerForConsecutiveRuns) determines a weight for run of consecutive tokens - each run is considered
		/// a single match, effectively. Each call to the first weight comber will have as many weights to combine as there are search terms, so if the "source" value is broken
		/// down into three words by the tokenBreaker then the weightCombinerForConsecutiveRuns will always be called with sets of three weights. The second weight combination
		/// is performed when multiple matches for a particular result must be combined to give a final match weight for that result.
		/// </summary>
		public static NonNullImmutableList<WeightedEntry<TKey>> GetConsecutiveMatches<TKey>(
			this IIndexData<TKey> index,
			string source,
			ITokenBreaker tokenBreaker,
			IndexGenerator.WeightedEntryCombiner weightCombinerForConsecutiveRuns,
			IndexGenerator.WeightedEntryCombiner weightCombinerForFinalMatches)
		{
            if (index == null)
                throw new ArgumentNullException("index");
            if (source == null)
                throw new ArgumentNullException("source");
            if (tokenBreaker == null)
                throw new ArgumentNullException("tokenBreaker");
            if (weightCombinerForConsecutiveRuns == null)
				throw new ArgumentNullException("weightCombinerForConsecutiveRuns");
			if (weightCombinerForFinalMatches == null)
				throw new ArgumentNullException("weightCombinerForFinalMatches");

			// If the token breaker won't actually translate the source value into multiple words then we can avoid all of the below work and just call index.GetMatches directly
			var weightAdjustedTokens = tokenBreaker.Break(source);
			if (weightAdjustedTokens.Count == 1)
				return index.GetMatches(source);

			// The index of this list will correspond to the index of the broken-down search terms
			var matchesForSearchTerms = new List<WeightedEntry<TKey>[]>();
			foreach (var weightAdjustedToken in weightAdjustedTokens)
			{
				matchesForSearchTerms.Add(
					index.GetMatches(weightAdjustedToken.Token).Select(w => new WeightedEntry<TKey>(
						w.Key,
						w.Weight * weightAdjustedToken.WeightMultiplier,
						w.SourceLocations
					)).ToArray()
				);
			}

			// For each match of the first search term, try to identify a run of token matches for the same key and source field. Any such runs will be recorded in the consecutiveMatches
			// list - these represent content segments that match the entirety of the search term (the "source" argument).
			var consecutiveMatches = new List<WeightedEntry<TKey>>();
			var searchTerms = new NonNullOrEmptyStringList(weightAdjustedTokens.Select(w => w.Token));
			foreach (var firstTermMatch in matchesForSearchTerms.First().SelectMany(m => BreakWeightedEntryIntoIndividualSourceLocations<TKey>(m)))
			{
				var matchesForEntireTerm = new NonNullImmutableList<WeightedEntry<TKey>>();
				matchesForEntireTerm = matchesForEntireTerm.Add(firstTermMatch);
				for (var termIndex = 1; termIndex < weightAdjustedTokens.Count; termIndex++)
				{
					var nTermMatch = matchesForSearchTerms[termIndex]
						.SelectMany(m => BreakWeightedEntryIntoIndividualSourceLocations<TKey>(m))
						.FirstOrDefault(m =>
							index.KeyComparer.Equals(m.Key, firstTermMatch.Key) &&
							(m.SourceLocations.First().SourceFieldIndex == firstTermMatch.SourceLocations.First().SourceFieldIndex) &&
							(m.SourceLocations.First().TokenIndex == firstTermMatch.SourceLocations.First().TokenIndex + termIndex)
						);
					if (nTermMatch == null)
						break;
					matchesForEntireTerm = matchesForEntireTerm.Add(nTermMatch);
				}
				if (matchesForEntireTerm.Count < weightAdjustedTokens.Count)
				{
					// If we didn't manage to get a full set of search terms then this isn't a full match
					continue;
				}

				// Combine the WeightedEntry instances that represent a run of individual matches (one for each word in the "source" argument) into a single WeightedEntry that represents
				// the entirety of the search term (each of the matchesForEntireTerm WeightedEntry instances will have only a single Source Location since the match data was split up
				// above by calling BreakWeightedEntryIntoIndividualSourceLocations before trying to find the consecutive matches)
				var sourceLocationOfFirstTerm = matchesForEntireTerm.First().SourceLocations.Single();
				var sourceLocationOfLastTerm = matchesForEntireTerm.Last().SourceLocations.Single();
				var matchWeightForConsecutiveRunEntry = weightCombinerForConsecutiveRuns(
					matchesForEntireTerm.Select(m => m.Weight).ToImmutableList()
				);
				consecutiveMatches.Add(
					new WeightedEntry<TKey>(
						matchesForEntireTerm.First().Key,
						matchWeightForConsecutiveRunEntry,
						new NonNullImmutableList<SourceFieldLocation>(new[]
						{
							// Since we're creating a new SourceFieldLocation instance that is derived from a run of multiple tokens, the TokenIndex is going to be an approximation -
							// taking the TokenIndex from the first search term probably makes the most sense. The SourceIndex and SourceTokenLength will be taken such that the entire
							// run is covered (from the start of the first search term to the end of the last). Since this is the only Source Location instance for the WeightedEntry,
							// its MatchWeightContribution value is equal to the WeightedEntry's Weight.
							new SourceFieldLocation(
								sourceLocationOfFirstTerm.SourceFieldIndex,
								sourceLocationOfFirstTerm.TokenIndex,
								sourceLocationOfFirstTerm.SourceIndex,
								(sourceLocationOfLastTerm.SourceIndex + sourceLocationOfLastTerm.SourceTokenLength) - sourceLocationOfFirstTerm.SourceIndex,
								matchWeightForConsecutiveRunEntry
							)
						})
					)
				);
			}
			
			// The matches need grouping by key before returning
			return consecutiveMatches
				.GroupBy(m => m.Key, index.KeyComparer)
				.Cast<IEnumerable<WeightedEntry<TKey>>>()
				.Select(matches => new WeightedEntry<TKey>(
					matches.First().Key,
					weightCombinerForFinalMatches(
						matches.Select(match => match.Weight).ToImmutableList()
					),
					matches.SelectMany(m => m.SourceLocations).ToNonNullImmutableList()
				))
				.ToNonNullImmutableList();
		}

		/// <summary>
		/// This GetConsecutiveMatches signature will call GetConsecutiveMatches specifying the DefaultConsecutiveRunsWeightCombiner and DefaultFinalMatchWeightCombiner
		/// for the weightCombiner arguments (the DefaultConsecutiveRunsWeightCombiner to calculate the combined weight of a run of tokens which should be considered as
		/// a single match and the DefaultFinalMatchWeightCombiner to combine all of these matches together for each result)
		/// </summary>
		public static NonNullImmutableList<WeightedEntry<TKey>> GetConsecutiveMatches<TKey>(this IIndexData<TKey> index, string source, ITokenBreaker tokenBreaker)
		{
			return GetConsecutiveMatches(index, source, tokenBreaker, DefaultConsecutiveRunsWeightCombiner, DefaultFinalMatchWeightCombiner);
		}

		/// <summary>
		/// This GetConsecutiveMatches signature will call GetConsecutiveMatches specifying the DefaultConsecutiveRunsWeightCombiner and DefaultFinalMatchWeightCombiner
		/// for the weightCombiner arguments and the DefaultTokenBreaker for the token breaker.
		/// </summary>
		public static NonNullImmutableList<WeightedEntry<TKey>> GetConsecutiveMatches<TKey>(this IIndexData<TKey> index, string source)
		{
			return GetConsecutiveMatches(index, source, DefaultTokenBreaker);
		}

		/// <summary>
		/// This will add up the match weights and multiply the result by two raised to the power of the number of consecutive search terms minus one, such that longer runs
		/// get more and more weight applied. So a run of two tokens gets their combined weight multipled by two, a run of three tokens gets the combined weight multiplied
		/// by four, four tokens get the combined weight multiplied by eight, etc..
		/// </summary>
		public static IndexGenerator.WeightedEntryCombiner DefaultConsecutiveRunsWeightCombiner
		{
			get
			{
				return weightsOfConsecutiveMatches => weightsOfConsecutiveMatches.Sum() * (int)Math.Pow(2, weightsOfConsecutiveMatches.Count - 1);
			}
		}

		/// <summary>
		/// This adds up the match weights
		/// </summary>
		public static IndexGenerator.WeightedEntryCombiner DefaultFinalMatchWeightCombiner
		{
			get
			{
				return weightsOfConsecutiveMatches => weightsOfConsecutiveMatches.Sum();
			}
		}

		/// <summary>
		/// Note: This is consistent with the default AutomatedIndexGeneratorFactoryBuilder TokenBreaker
		/// </summary>
		public static ITokenBreaker DefaultTokenBreaker
		{
			get
			{
				return new WhiteSpaceExtendingTokenBreaker(
					new ImmutableList<char>(new[] {
						'<', '>', '[', ']', '(', ')', '{', '}',
						'.', ',', ':', ';', '"', '?', '!',
						'/', '\\',
						'@', '+', '|', '='
					}),
					new WhiteSpaceTokenBreaker()
				);
			}
		}

		/// <summary>
		/// This will return WeightedEntry instances where each have precisely one SourceLocation
		/// </summary>
		private static NonNullImmutableList<WeightedEntry<TKey>> BreakWeightedEntryIntoIndividualSourceLocations<TKey>(WeightedEntry<TKey> match)
		{
			if (match == null)
				throw new ArgumentNullException("match");

			var splitMatches = new NonNullImmutableList<WeightedEntry<TKey>>();
			if (match.SourceLocations.Count == 1)
			{
				splitMatches = splitMatches.Add(match);
				return splitMatches;
			}
			foreach (var sourceLocation in match.SourceLocations)
			{
				// Use the MatchWeightContribution of each Source Location entry rather than the combined match Weight since we're splitting the
				// match data back up
				splitMatches = splitMatches.Add(
					new WeightedEntry<TKey>(
						match.Key,
						sourceLocation.MatchWeightContribution,
						new NonNullImmutableList<SourceFieldLocation>(new[] { sourceLocation })
					)
				);
			}
			return splitMatches;
		}
	}
}
