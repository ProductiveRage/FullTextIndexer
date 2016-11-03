using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace FullTextIndexer.Core.Indexes.TernarySearchTree
{
    /// <summary>
    /// This will perform string comparisons where the values have any accented characters replaced with non-accented versions, all whitespace converted to spaces and runs of
    /// whitespace replaced with a single space, all punctuation removed and the content then lowercased.
    /// </summary>
    public sealed class DefaultStringNormaliser : StringNormaliser
    {
        private readonly static HashSet<Char> PunctuationCharacters = new HashSet<char>(
            Enumerable.Range(char.MinValue, char.MaxValue).Select(c => (char)c).Where(c => char.IsPunctuation(c))
        );

        public override string GetNormalisedString(string value)
        {
                if (value == null)
                throw new ArgumentNullException("value");

            var normalisedValue = value.Normalize(NormalizationForm.FormKD);
            var content = new char[normalisedValue.Length];
            var contentIndex = 0;
            var contentIndexOfLastNonWhitespace = 0;
            var lastCharWasWhitespace = false;
            var gotContent = false;
            for (var index = 0; index < normalisedValue.Length; index++)
            {
                var currentChar = normalisedValue[index];
                if (PunctuationCharacters.Contains(currentChar))
                    continue;
                if ((currentChar == '\r') || (currentChar == '\n') || (currentChar == '\t'))
                    currentChar = ' ';
                else
                {
                    var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(currentChar);
                    if ((unicodeCategory == UnicodeCategory.EnclosingMark)
                    || (unicodeCategory == UnicodeCategory.NonSpacingMark)
                    || (unicodeCategory == UnicodeCategory.SpacingCombiningMark))
                        currentChar = ' ';
                }
                if (currentChar == ' ')
                {
                    if (!lastCharWasWhitespace && gotContent)
                    {
                        content[contentIndex] = currentChar;
                        contentIndex++;
                        lastCharWasWhitespace = true;
                    }
                    continue;
                }
                if (!char.IsLower(currentChar))
                    currentChar = char.ToLower(currentChar);
                content[contentIndex] = currentChar;
                contentIndex++;
                contentIndexOfLastNonWhitespace = contentIndex;
                lastCharWasWhitespace = false;
                gotContent = true;
            }
            return new string(content, 0, contentIndexOfLastNonWhitespace);
        }
    }
}
