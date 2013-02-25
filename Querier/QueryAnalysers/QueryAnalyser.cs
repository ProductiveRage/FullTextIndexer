/*
using System;
using Common.Lists;
using Querier.QueryAnalysers.ContentAnalysers;
using Querier.QueryAnalysers.StringNavigators;
using Querier.QuerySegments;

namespace Querier.QueryAnalysers
{
	public class QueryAnalyser : IAnalyseQueries
	{
		/// <summary>
		/// This will never return null, it will throw an exception for a null or blank search term
		/// </summary>
		public IQuerySegment Analyse(string search)
		{
			if (string.IsNullOrWhiteSpace(search))
				throw new ArgumentException("Null/blank search specified");

			IAnalyseCharacterLevelContent analyser = new BreakPointCharacterAnalyser(new NonNullImmutableList<IQuerySegment>());
			IWalkThroughStrings stringNavigator = new StringNavigator(search);
			while (true)
			{
				var characterResult = analyser.Process(stringNavigator);
				if (characterResult.Query != null)
					return characterResult.Query;

				stringNavigator = stringNavigator.Next;
			}
		}
	}
}
*/