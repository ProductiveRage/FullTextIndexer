﻿using System;
using System.Collections.Generic;
using System.Text;
using FullTextIndexer.Common.Lists;
using FullTextIndexer.Querier.QueryAnalysers.StringNavigators;
using FullTextIndexer.Querier.QuerySegments;

namespace FullTextIndexer.Querier.QueryAnalysers.ContentAnalysers
{
	public class ContentSectionCharacterAnalyser
	{
		private readonly HashSet<char> _terminationCharacters;
		private readonly ContentToQuerySegmentTranslator _contentToQuerySegmentTranslator;
		public ContentSectionCharacterAnalyser(ImmutableList<char> terminationCharacters, ContentToQuerySegmentTranslator contentToQuerySegmentTranslator)
		{
			if (terminationCharacters == null)
				throw new ArgumentNullException(nameof(terminationCharacters));
			if (terminationCharacters.Contains('\\'))
				throw new ArgumentException("may not contain \\ as this is reserved as an escape character", nameof(terminationCharacters));
			_terminationCharacters = new HashSet<char>(terminationCharacters);
			_contentToQuerySegmentTranslator = contentToQuerySegmentTranslator ?? throw new ArgumentNullException(nameof(contentToQuerySegmentTranslator));
		}

		/// <summary>
		/// This will never be called with a null or blank content value and must never return null
		/// </summary>
		public delegate IQuerySegment ContentToQuerySegmentTranslator(string content);

		public ProcessedQuerySegment Process(IWalkThroughStrings stringNavigator)
		{
			if (stringNavigator == null)
				throw new ArgumentNullException(nameof(stringNavigator));

			var contentBuilder = new StringBuilder();
			var processNextCharacterStrictlyAsContent = false;
			while (true)
			{
				if (stringNavigator.CurrentCharacter == null)
				{
					var content = contentBuilder.ToString();
					return new ProcessedQuerySegment(
						stringNavigator,
						(content == "") ? (IQuerySegment)new NoMatchContentQuerySegment() : _contentToQuerySegmentTranslator(content)
					);
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
					else if (_terminationCharacters.Contains(stringNavigator.CurrentCharacter.Value))
					{
						var content = contentBuilder.ToString();
						return new ProcessedQuerySegment(
							stringNavigator,
							(content == "") ? (IQuerySegment)new NoMatchContentQuerySegment() : _contentToQuerySegmentTranslator(content)
						);
					}
				}

				contentBuilder.Append(stringNavigator.CurrentCharacter.Value);
				stringNavigator = stringNavigator.Next;
			}
		}
	}
}
