using System;

namespace Tester.KeyVariants
{
    public class LanguageScopedIndexKey : IIndexKey
    {
        public LanguageScopedIndexKey(int productKey, int languageKey)
        {
            ProductKey = productKey;
            LanguageKey = languageKey;
        }
        public int ProductKey { get; private set; }
        public int LanguageKey { get; private set; }
        public bool IsApplicableFor(int languageKey, int channelKey)
        {
            return (languageKey == LanguageKey); // This isn't scoped to any channel so it's a match if the language is a match
        }
        public bool Equals(IIndexKey obj)
        {
            if (obj == null || (obj.GetType() != GetType()))
                return false;
            return ((obj.ProductKey == ProductKey) && (((LanguageScopedIndexKey)obj).LanguageKey == LanguageKey));
        }
        public override bool Equals(object obj)
        {
            return Equals(obj as IIndexKey);
        }
        public override int GetHashCode()
        {
            return String.Format("LanguageScopedIndexKey-{0}-{1}", ProductKey, LanguageKey).GetHashCode();
        }
        public override string ToString()
        {
            return String.Format("{0}:{1}-{2}", base.ToString(), ProductKey, LanguageKey);
        }
    }
}
