using System;
using System.Collections.Generic;
using System.Text;
using FullTextIndexer.Common.Lists;
using FullTextIndexer.Core.Indexes;

namespace FullTextIndexer.Core.TokenBreaking
{
	/// <summary>
	/// This will break tokens on any whitespace character, any resulting empty entries will be ignored
	/// </summary>
#if NET452
	[Serializable]
#endif
	public class WhiteSpaceTokenBreaker : ITokenBreaker
	{
		private readonly ITokenBreaker _optionalWrappedTokenBreaker;
		public WhiteSpaceTokenBreaker(ITokenBreaker optionalWrappedTokenBreaker)
		{
			_optionalWrappedTokenBreaker = optionalWrappedTokenBreaker;
		}
		public WhiteSpaceTokenBreaker() : this(null) { }

		/// <summary>
		/// This will never return null. It will throw an exception for null input.
		/// </summary>
		public NonNullImmutableList<WeightAdjustingToken> Break(string value)
		{
			if (value == null)
				throw new ArgumentNullException("value");

			IEnumerable<WeightAdjustingToken> tokensToBreak;
			if (_optionalWrappedTokenBreaker == null)
				tokensToBreak = new[] { new WeightAdjustingToken(value, 1, new SourceLocation(0, 0, value.Length)) };
			else
				tokensToBreak = _optionalWrappedTokenBreaker.Break(value);

			var tokens = new List<WeightAdjustingToken>();
			foreach (var weightAdjustingToken in tokensToBreak)
			{
				var buffer = new StringBuilder();
				var bufferStartIndex = 0;
				for (var index = 0; index < weightAdjustingToken.Token.Length; index++)
				{
					if (char.IsWhiteSpace(weightAdjustingToken.Token[index]))
					{
						if (buffer.Length > 0)
						{
							var bufferContents = buffer.ToString();
							tokens.Add(new WeightAdjustingToken(
								bufferContents,
								weightAdjustingToken.WeightMultiplier,
								new SourceLocation(
									tokens.Count,
									weightAdjustingToken.SourceLocation.SourceIndex + bufferStartIndex,
									bufferContents.Length
								)
							));
							buffer.Clear();
						}
						bufferStartIndex = index + 1;
						continue;
					}

					buffer.Append(weightAdjustingToken.Token[index]);
				}
				if (buffer.Length > 0)
				{
					var bufferContents = buffer.ToString();
					tokens.Add(new WeightAdjustingToken(
						bufferContents,
						weightAdjustingToken.WeightMultiplier,
						new SourceLocation(
							tokens.Count,
							weightAdjustingToken.SourceLocation.SourceIndex + bufferStartIndex,
							bufferContents.Length
						)
					));
					buffer.Clear();
				}
			};
			return tokens.ToNonNullImmutableList();
		}
	}		
}
