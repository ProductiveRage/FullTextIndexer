using System;
using Tester.Example2.SourceData;

namespace Tester.Example2.KeyVariants
{
    public sealed class LanguageScopedIndexKey : IIndexKey
    {
        public LanguageScopedIndexKey(int productKey, LanguageDetails language)
        {
            if (language == null)
                throw new ArgumentNullException("language");

            ProductKey = productKey;
            Language = language;
        }

        public int ProductKey { get; private set; }

        /// <summary>
        /// This will never be null
        /// </summary>
        public LanguageDetails Language { get; private set; }

        /// <summary>
        /// This will throw an exception for a null language reference
        /// </summary>
        public bool IsApplicableFor(LanguageDetails language, int channelKey)
        {
            if (language == null)
                throw new ArgumentNullException("language");

            return language.Equals(Language); // This isn't scoped to any channel so it's a match if the language is a match
        }

        public bool Equals(IIndexKey obj)
        {
            var objLanguageScopedIndexKey = obj as LanguageScopedIndexKey;
            if (objLanguageScopedIndexKey == null)
                return false;

            return ((objLanguageScopedIndexKey.ProductKey == ProductKey) && objLanguageScopedIndexKey.Language.Equals(Language));
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as IIndexKey);
        }

        public override int GetHashCode()
        {
            // Since the overridden ToString method will consistently encapsulate all of the information for this instance
            // we use it to override the GetHashCode method, consistent with the overridden Equals implementation
            return ToString().GetHashCode();
        }

        public override string ToString()
        {
            return String.Format("{0}:{1}-{2}", base.ToString(), ProductKey, Language.Key);
        }
    }
}
