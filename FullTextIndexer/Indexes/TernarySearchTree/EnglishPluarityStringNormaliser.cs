using System;

namespace FullTextIndexer.Indexes.TernarySearchTree
{
    /// <summary>
    /// This will match common strings where one is the plural and the other the singular version of the same word. It not intended to be perfect and may
    /// match a few false positives, but it should catch most of the most common cases.
    /// </summary>
    [Serializable]
    public class EnglishPluarityStringNormaliser : StringNormaliser
    {
        public override string GetNormalisedString(string value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            value = value.Trim().ToLower();

            var suffixSets = new[]
            {
                new[] { "ex", "exes", "ices" }, // eg. index / indices / indexes
                new[] { "ula", "ulae", "ulas" }, // eg. formula / formulae / formulas
                new[] { "y", "ies" }, // eg. category / categories
                new[] { "us", "ii" }, // eg. cactus / cactii
                new[] { "ld", "ldren" }, // eg. child / children
                new[] { "man", "men" }, // eg. woman / women
                new[] { "dataum", "data" }, // Common special case
                new[] { "medium", "media" }, // Common special case
                new[] { "ses", "es", "s" } // eg. CMSes (more common with anacronyms), matching "s" here means we must use "ses", "es" AND "s" as fallbacks below
            };
            foreach (var suffixSet in suffixSets)
            {
                foreach (var suffix in suffixSet)
                {
                    if (value.EndsWith(suffix))
                    {
                        return string.Format(
                            "{0}[{1}]",
                            value.Substring(0, value.Length - suffix.Length),
                            string.Join("][", suffixSet)
                        );
                    }
                }
            }

            // If no irregulare suffixes match then append all of "ses", "es" and "s" to catch other common cases (and ensure that we match anything that ends
            // in "s" due to the suffix set "ses", "es", "s" above - we need to ensure that "cat" is transformed to "cat[ses][es][s]" in order to match "cats"
            // which will get that form applied above).
            return value + "[ses][es][s]";
        }
    }
}
