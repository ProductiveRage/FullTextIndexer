using System;

namespace FullTextIndexer.Core.Indexes
{
	/// <summary>
	/// This SourceFieldLocation derivation annotates the data with the search term that was matched - this is included in the data that is returned from the
	/// GetPartialMatches extension method but would not be recorded in an index
	/// </summary>
	[Serializable]
	public class SourceFieldLocationWithTerm : SourceFieldLocation
	{
		public SourceFieldLocationWithTerm(int sourceFieldIndex, int tokenIndex, int sourceIndex, int sourceTokenLength, string searchTerm)
			: base(sourceFieldIndex, tokenIndex, sourceIndex, sourceTokenLength)
		{
			if (string.IsNullOrWhiteSpace(searchTerm))
				throw new ArgumentException("Null/blank searchTerm specified");

			SearchTerm = searchTerm;
		}

		/// <summary>
		/// This will never be null or blank
		/// </summary>
		public string SearchTerm { get; private set; }
	}
}
