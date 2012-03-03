using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Common.Lists;

namespace FullTextIndexer.TokenBreaking
{
    /// <summary>
    /// This will replace all accented characters with non-accented equivalents
    /// </summary>
    public class AccentReplacingTokenBreaker : ITokenBreaker
    {
        private ITokenBreaker _tokenBreaker;
        public AccentReplacingTokenBreaker(ITokenBreaker tokenBreaker)
        {
            if (tokenBreaker == null)
                throw new ArgumentNullException("tokenBreaker");

            _tokenBreaker = tokenBreaker;
        }

        /// <summary>
        /// This will never return null. It will throw an exception for null input.
        /// </summary>
        public NonNullOrEmptyStringList Break(string value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            return new NonNullOrEmptyStringList(
                _tokenBreaker.Break(value).Select(v => RemoveDiacritics(v).Trim()).Where(v => v != "")
            );
        }

        private string RemoveDiacritics(string value)
        {
            var normalisedValue = value.Normalize(NormalizationForm.FormD);
            var content = new StringBuilder();
            for (var index = 0; index < normalisedValue.Length; index++)
            {
                var currenctChar = normalisedValue[index];
                if (CharUnicodeInfo.GetUnicodeCategory(currenctChar) != UnicodeCategory.NonSpacingMark)
                    content.Append(currenctChar);
            }
            return (content.ToString().Normalize(NormalizationForm.FormC));
        }
    }
}
