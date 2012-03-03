using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Common.StringComparisons
{
    public class CaseInsensitiveAccentReplacingPunctuationRemovingStringComparer : IEqualityComparer<string>
    {
        public bool Equals(string x, string y)
        {
            if (x == null)
                throw new ArgumentNullException("x");
            if (y == null)
                throw new ArgumentNullException("y");

            return ReduceString(x) == ReduceString(y);
        }

        public int GetHashCode(string obj)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");

            return ReduceString(obj).GetHashCode();
        }

        private string ReduceString(string value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            return RemoveDiacritics(RemovePunctuation(value)).ToLower();
        }

        private static Regex PunctuationRemover = new Regex("\\p{P}+", RegexOptions.Compiled);
        private string RemovePunctuation(string value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            return PunctuationRemover.Replace(value, "");
        }

        private string RemoveDiacritics(string value)
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
