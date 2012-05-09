using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Common.StringComparisons
{
    /// <summary>
    /// This will perform string comparisons where the values have any accented characters replaced with non-accented versions, all whitespace converted to spaces and runs of
    /// whitespace replaced with a single space, all punctuation removed and the content then lowercased.
    /// </summary>
    [Serializable]
    public class CaseInsensitiveAccentReplacingPunctuationRemovingWhitespaceStandardisingStringComparer : IEqualityComparer<string>
    {
        public bool Equals(string x, string y)
        {
            if (x == null)
                throw new ArgumentNullException("x");
            if (y == null)
                throw new ArgumentNullException("y");

            return NormaliseString(x) == NormaliseString(y);
        }

        public int GetHashCode(string obj)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");

            return NormaliseString(obj).GetHashCode();
        }

        public static string NormaliseString(string value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            return RemoveDiacritics(
                RemovePunctuation(
                    StandardiseWhitespace(value)
                )
            ).ToLower();
        }

        private static Regex WhitespaceMatcher = new System.Text.RegularExpressions.Regex("\\s+", RegexOptions.Compiled);
        private static string StandardiseWhitespace(string value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            return WhitespaceMatcher.Replace(value, " ").Trim();
        }


        private static Regex PunctuationRemover = new Regex("\\p{P}+", RegexOptions.Compiled);
        private static string RemovePunctuation(string value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            return PunctuationRemover.Replace(value, "");
        }

        private static string RemoveDiacritics(string value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

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
