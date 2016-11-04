using System;

namespace FullTextIndexer.Core.Indexes
{
#if NET452
    [Serializable]
#endif
	public class SourceLocation
	{
		public SourceLocation(int tokenIndex, int sourceIndex, int sourceTokenLength)
		{
			if (tokenIndex < 0)
				throw new ArgumentOutOfRangeException("tokenIndex", "must be zero or greater");
			if (sourceIndex < 0)
				throw new ArgumentOutOfRangeException("sourceIndex", "must be zero or greater");
			if (sourceTokenLength <= 0)
				throw new ArgumentOutOfRangeException("sourceTokenLength", "must be greater than zero");

			TokenIndex = tokenIndex;
			SourceIndex = sourceIndex;
			SourceTokenLength = sourceTokenLength;
		}

		/// <summary>
		/// This is the index of the token in the Tokens set for the source content (so this it will be two if, for example, it is the third token in the extract tokens
		/// for a given content string). It will always be zero or greater.
		/// </summary>
		public int TokenIndex { get; private set; }

		/// <summary>
		/// This is the index in the source content at which the token starts, it will always be zero or greater
		/// </summary>
		public int SourceIndex { get; private set; }

		/// <summary>
		/// This is the length of the token in the source content. It may not be the same as the length of the Token string since the source may have been manipulated
		/// in order to reterieve the Token value recorded here, this is the length of the content before any manipulation. This will always be greater than zero.
		/// </summary>
		public int SourceTokenLength { get; private set; }
	}
}
