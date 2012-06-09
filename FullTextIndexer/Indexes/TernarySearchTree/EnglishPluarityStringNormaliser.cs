using System;
using System.Collections.Generic;
using System.Linq;

namespace FullTextIndexer.Indexes.TernarySearchTree
{
    /// <summary>
    /// This will match common strings where one is the plural and the other the singular version of the same word. It not intended to be perfect and may
    /// match a few false positives, but it should catch most of the most common cases.
    /// </summary>
    [Serializable]
    public class EnglishPluarityStringNormaliser : StringNormaliser
    {
        private IStringNormaliser _optionalPreNormaliser;
        public EnglishPluarityStringNormaliser(IStringNormaliser optionalPreNormaliser)
        {
            _optionalPreNormaliser = optionalPreNormaliser;
        }
        public EnglishPluarityStringNormaliser() : this(null) { }

        public override string GetNormalisedString(string value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            // If an additional normaliser was specified in the constructor then process the string with that first (eg. a normaliser that removes punctuation
            // from values may be beneficial depending upon the the content that may be passed in)
            if (_optionalPreNormaliser != null)
                value = _optionalPreNormaliser.GetNormalisedString(value);

            // We need to trim the string since we will be testing string endings and any trailing whitespace could interfere with that matching
            value = value.Trim();
            if (value == "")
                return "";

            // Need to lower case the value since the suffix comparisons are all to lower case characters
            value = value.ToLower();
            foreach (var matcher in Matchers)
            {
                string valueTransformed;
                if (matcher.TryToTransform(value, out valueTransformed))
                    return valueTransformed;
            }

            // If no irregulare suffixes match then append all of "ses", "es" and "s" to catch other common cases (and ensure that we match anything that ends
            // in "s" due to the suffix set "ses", "es", "s" above - we need to ensure that "cat" is transformed to "cat[ses][es][s]" in order to match "cats"
            // which will get that form applied above).
            return value + "[ses][es][s]";
        }

        private readonly static PluralEntry[] Matchers = new[]
        {
            // eg. index / indexes / indices
            new PluralEntry(new[] { "ex", "exes", "ices" }, MatchTypeOptions.SuffixOnly),

            // eg. formula / formulae / formulas
            new PluralEntry(new[] { "ula", "ulae", "ulas" }, MatchTypeOptions.SuffixOnly),

            // eg. category / categories
            new PluralEntry(new[] { "y", "ies" }, MatchTypeOptions.SuffixOnly),

            // eg. cactus / cactii
            new PluralEntry(new[] { "us", "ii" }, MatchTypeOptions.SuffixOnly),

            // eg. child / children
            new PluralEntry(new[] { "ld", "ldren" }, MatchTypeOptions.SuffixOnly),

            // eg. medium / media
            new PluralEntry(new[] { "ium", "ia" }, MatchTypeOptions.SuffixOnly),

            // eg. CMSes (more common with anacronyms), matching "s" here means we must use "ses", "es" AND "s" as fallbacks below
            new PluralEntry(new[] { "ses", "es", "s" }, MatchTypeOptions.SuffixOnly),

            // Common special cases
            new PluralEntry(new[] { "datum", "data" }, MatchTypeOptions.WholeWord),
            new PluralEntry(new[] { "man", "men" }, MatchTypeOptions.WholeWord),
            new PluralEntry(new[] { "woman", "women" }, MatchTypeOptions.WholeWord)
        };

        [Serializable]
        private class PluralEntry
        {
            private HashSet<string> _values;
            private string _combinedValues;
            private MatchTypeOptions _matchType;
            public PluralEntry(IEnumerable<string> values, MatchTypeOptions matchType)
            {
                if (values == null)
                    throw new ArgumentNullException("values");
                if (!Enum.IsDefined(typeof(MatchTypeOptions), matchType))
                    throw new ArgumentOutOfRangeException("matchType");

                // Using a case-insensitive comparer means that there will be no need to adjust the case of input values in TryToTransform
                var valuesTidied = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
                foreach (var value in values)
                {
                    var valueTrimmed = (value ?? "").Trim();
                    if (valueTrimmed == "")
                        throw new ArgumentException("Null/blank entry encountered in values");

                    if (!valuesTidied.Contains(valueTrimmed))
                        valuesTidied.Add(valueTrimmed);
                }
            
                _values = valuesTidied;
                _combinedValues = "[" + string.Join("][", valuesTidied) + "]";
                _matchType = matchType;
            }

            public bool TryToTransform(string value, out string valueTransformed)
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                if (_matchType == MatchTypeOptions.SuffixOnly)
                {
                    var suffixMatch = _values.FirstOrDefault(v => (value.Length > v.Length) && value.EndsWith(v));
                    if (suffixMatch != null)
                    {
                        valueTransformed = value.Substring(0, value.Length - suffixMatch.Length) + _combinedValues;
                        return true;
                    }
                }
                else
                {
                    if (_values.Contains(value))
                    {
                        valueTransformed = _combinedValues;
                        return true;
                    }
                }
                valueTransformed = null;
                return false;
            }
        }

        [Serializable]
        public enum MatchTypeOptions
        {
            SuffixOnly,
            WholeWord
        }
    }
}
