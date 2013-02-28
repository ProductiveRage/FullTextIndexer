using System;
using System.Linq;
using FullTextIndexer.Common.Lists;
using FullTextIndexer.Querier.QueryAnalysers.StringNavigators;
using FullTextIndexer.Querier.QuerySegments;

namespace FullTextIndexer.Querier.QueryAnalysers.ContentAnalysers
{
	public class BreakPointCharacterAnalyser
	{
		private readonly int _bracketingLevel;
		private readonly SingleSegmentRestrictionOptions _singleSegmentRestriction;
		private BreakPointCharacterAnalyser(int bracketingLevel, SingleSegmentRestrictionOptions singleSegmentRestriction)
		{
			if (bracketingLevel < 0)
				throw new ArgumentOutOfRangeException("bracketingLevel", "must be zero or greater");
			if (!Enum.IsDefined(typeof(SingleSegmentRestrictionOptions), singleSegmentRestriction))
				throw new ArgumentOutOfRangeException("singleSegmentRestriction");
			
			// In order to process a single-segment stretch of content, the bracketingLevel must be (re)set to zero otherwise the bracketing count
			// may fall out of sync since the loop in the Process method may exit before encountering a close brace. (This configuration is used
			// when a "+" or "-" symbol is enountered, indicating that the next single segment should be wrapped in a CompulsoryQuerySegment or
			// ExcludingQuerySegment - these can be nested inside bracketed sections or can be applied TO bracketed sections, but when we are
			// identifying the single segment, the bracket count at that point must be reset - it will be picked up again by the parent
			// BreakPointCharacterAnalyser instance in the case of a nested "+" or "-" segment).
			if ((singleSegmentRestriction == SingleSegmentRestrictionOptions.RetrieveSingleSegmentOnly) && (bracketingLevel != 0))
				throw new ArgumentException("If SingleSegmentRestrictionOptions.RetrieveSingleSegmentOnly is specified then the bracketingLevel must be reset to zero");
			
			_bracketingLevel = bracketingLevel;
			_singleSegmentRestriction = singleSegmentRestriction;
		}
		public BreakPointCharacterAnalyser() : this(0, SingleSegmentRestrictionOptions.DefaultBehaviour) { }

		private enum SingleSegmentRestrictionOptions
		{
			DefaultBehaviour,
			RetrieveSingleSegmentOnly
		}

		public ProcessedQuerySegment Process(IWalkThroughStrings stringNavigator)
		{
			if (stringNavigator == null)
				throw new ArgumentNullException("stringNavigator");

			var querySegments = new NonNullImmutableList<IQuerySegment>();
			var processNextCharacterStrictlyAsContent = false;
			while (stringNavigator.CurrentCharacter != null)
			{
				// If this analyser instance only has to process a single segment and this loop has already achieved that then break out now
				if ((_singleSegmentRestriction == SingleSegmentRestrictionOptions.RetrieveSingleSegmentOnly) && querySegments.Any())
					break;

				// Encountered some whitespace, skip over it
				if (char.IsWhiteSpace(stringNavigator.CurrentCharacter.Value))
				{
					stringNavigator = stringNavigator.Next;
					continue;
				}

				if (processNextCharacterStrictlyAsContent)
					processNextCharacterStrictlyAsContent = false;
				else
				{
					if (stringNavigator.CurrentCharacter == '\\')
					{
						processNextCharacterStrictlyAsContent = true;
						stringNavigator = stringNavigator.Next;
						continue;
					}

					// A "+" symbol means that the next segment is compulsory and should be wrapped in a CompulsoryQuerySegment (only the NEXT segment, so
					// the SingleSegmentRestrictionOptions.RetrieveSingleSegmentOnly configuration is specified for the new BreakPointCharacterAnalyser)
					if (stringNavigator.CurrentCharacter == '+')
					{
						var compulsorySectionProcessor = new BreakPointCharacterAnalyser(0, SingleSegmentRestrictionOptions.RetrieveSingleSegmentOnly);
						var compulsorySectionResult = compulsorySectionProcessor.Process(stringNavigator.Next);
						querySegments = querySegments.Add(
							new CompulsoryQuerySegment(
								compulsorySectionResult.QuerySegment
							)
						);
						stringNavigator = compulsorySectionResult.StringNavigator;
						continue;
					}

					// A "-" symbol means that the next segment should be excluded from results
					if (stringNavigator.CurrentCharacter == '-')
					{
						var compulsorySectionProcessor = new BreakPointCharacterAnalyser(0, SingleSegmentRestrictionOptions.RetrieveSingleSegmentOnly);
						var compulsorySectionResult = compulsorySectionProcessor.Process(stringNavigator.Next);
						querySegments = querySegments.Add(
							new ExcludingQuerySegment(
								compulsorySectionResult.QuerySegment
							)
						);
						stringNavigator = compulsorySectionResult.StringNavigator;
						continue;
					}

					// If an opening brace is encountered then the following content needs processing separately and wrapping into a single query segment
					if (stringNavigator.CurrentCharacter == '(')
					{
						var bracketedSectionProcessor = new BreakPointCharacterAnalyser(_bracketingLevel + 1, SingleSegmentRestrictionOptions.DefaultBehaviour);
						var bracketedSectionResult = bracketedSectionProcessor.Process(stringNavigator.Next);
						querySegments = querySegments.Add(bracketedSectionResult.QuerySegment);
						stringNavigator = bracketedSectionResult.StringNavigator.Next; // Skip over the closing bracket (if hit end of the content then this won't do any harm)
						continue;
					}

					// If a closing brace is encountered then (so long as the content is valid), we'll want to exit this loop since the processing for the
					// nested section we must be inside has completed
					if (stringNavigator.CurrentCharacter == ')')
					{
						// If we're inside a bracketed section and encounter a close bracket then we can't do any more processing inside this loop. If NOT
						// inside a bracketed section when a close bracket is encountered then it's invalid content - just ignore the bracket and move on.
						if (_bracketingLevel > 0)
							break;

						stringNavigator = stringNavigator.Next;
						continue;
					}

					// A quote character means that the following content needs to be processed as a string of content and wrapped in a PreciseMatchQuerySegment
					if (stringNavigator.CurrentCharacter == '"')
					{
						var quotedContentProcessor = new ContentSectionCharacterAnalyser(
							new ImmutableList<char>(new[] { '"' }),
							content => new PreciseMatchQuerySegment(content)
						);
						var quotedContentSectionResult = quotedContentProcessor.Process(stringNavigator.Next);
						querySegments = querySegments.Add(quotedContentSectionResult.QuerySegment);
						stringNavigator = quotedContentSectionResult.StringNavigator.Next; // Skip over the closing quote
						continue;
					}
				}

				// Hit some actual content, start processing it using a ContentSectionCharacterAnalyser
				// - Opening and closing braces are specified as termination characters for the analyser as processing should return here if either or them are
				//   encountered (as it indicates that the content segment that was being processed has ended and a nested section is starting or ending, which
				//   should be handled by this)
				var contentProcessor = new ContentSectionCharacterAnalyser(
					_whiteSpaceCharacters.AddRange(new ImmutableList<char>(new[] { '(', ')' })),
					content => new StandardMatchQuerySegment(content)
				);
				var contentSectionResult = contentProcessor.Process(stringNavigator);
				querySegments = querySegments.Add(contentSectionResult.QuerySegment);
				stringNavigator = contentSectionResult.StringNavigator;
				continue;
			}
			return new ProcessedQuerySegment(
				stringNavigator,
				querySegments.ToSingleQuerySegment()
			);
		}

		private readonly static ImmutableList<char> _whiteSpaceCharacters =
			Enumerable.Range((int)char.MinValue, (int)char.MaxValue)
				.Select(v => (char)v)
				.Where(c => char.IsWhiteSpace(c))
				.ToImmutableList();
	}
}
