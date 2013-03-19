using System;

namespace FullTextIndexer.Core.Indexes
{
	[Serializable]
	public class SourceFieldLocation : SourceLocation
	{
		public SourceFieldLocation(int sourceFieldIndex, int tokenIndex, int sourceIndex, int sourceTokenLength) : base(tokenIndex, sourceIndex, sourceTokenLength)
		{
			if (sourceFieldIndex < 0)
				throw new ArgumentOutOfRangeException("sourceFieldIndex", "must be zero or greater");

			SourceFieldIndex = sourceFieldIndex;
		}

		/// <summary>
		/// This is the index of the field in the source content that the token was extracted from. Only fields that have content are considered so the field index values
		/// may not appear consistent for data across two instances of the same data type but the TokenIndex values describes the position within a particular value extracted
		/// from content (so there may be two SourceLocation instances with TokenIndex zero if data was extracted from multiple fields of a source data instance, for example).
		/// This will always be zero or greater.
		/// </summary>
		public int SourceFieldIndex { get; private set; }
	}
}
