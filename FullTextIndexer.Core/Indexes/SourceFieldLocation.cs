using System;

namespace FullTextIndexer.Core.Indexes
{
	/// <summary>
	/// This extends the SourceLocation class with additional data - the index of the field that the content was extracted from and the match weight contribution of the extracted
	/// content. The weight contribution may depend on multiple factors, one of which is the field that it is extracted from (some properties on a source object may be given a
	/// weight multiplier to give them greater or lesser match weights). The weight contribution may also depend upon the content extracted (the Index Generator may be
	/// configured to give lower weight to stop words, for example). A WeightedEntry may have multiple SourceFieldLocation instances, its Weight value represents a
	/// combined value that is calculated from the MatchWeightContribution values of its Source Locations.
	/// </summary>
#if NET45
    [Serializable]
#endif
	public class SourceFieldLocation : SourceLocation
	{
		public SourceFieldLocation(int sourceFieldIndex, int tokenIndex, int sourceIndex, int sourceTokenLength, float matchWeightContribution) : base(tokenIndex, sourceIndex, sourceTokenLength)
		{
			if (sourceFieldIndex < 0)
				throw new ArgumentOutOfRangeException("sourceFieldIndex", "must be zero or greater");
			if (matchWeightContribution <= 0)
				throw new ArgumentOutOfRangeException("matchWeightContribution", "must be greater than zero");

			MatchWeightContribution = matchWeightContribution;
			SourceFieldIndex = sourceFieldIndex;
		}

		/// <summary>
		/// This is the index of the field in the source content that the token was extracted from. Only fields that have content are considered so the field index values
		/// may not appear consistent for data across two instances of the same data type but the TokenIndex values describes the position within a particular value extracted
		/// from content (so there may be two SourceLocation instances with TokenIndex zero if data was extracted from multiple fields of a source data instance, for example).
		/// This will always be zero or greater.
		/// </summary>
		public int SourceFieldIndex { get; private set; }

		/// <summary>
		/// This is the contribution of the source segment to the final Token match weight (how that combined match weight is determined depends upon the Index Generator,
		/// it could be a sum or an average, for example, or something else entirely)
		/// </summary>
		public float MatchWeightContribution { get; private set; }
	}
}
