using System;

namespace Tester.KeyVariants
{
    public class LanguageAndChannelScopedIndexKey : IIndexKey
    {
        public LanguageAndChannelScopedIndexKey(int productKey, int languageKey, int channelKey)
        {
            ProductKey = productKey;
            LanguageKey = languageKey;
            ChannelKey = channelKey;
        }
        public int ProductKey { get; private set; }
        public int LanguageKey { get; private set; }
        public int ChannelKey { get; private set; }
        public bool IsApplicableFor(int languageKey, int channelKey)
        {
            return ((languageKey == LanguageKey) && (channelKey == ChannelKey));
        }
        public bool Equals(IIndexKey obj)
        {
            if (obj == null || (obj.GetType() != GetType()))
                return false;
            var languageAndChannelScopedIndexKeyObj = (LanguageAndChannelScopedIndexKey)obj;
            return (
                (obj.ProductKey == ProductKey) &&
                (languageAndChannelScopedIndexKeyObj.LanguageKey == LanguageKey) &&
                (languageAndChannelScopedIndexKeyObj.ChannelKey == ChannelKey)
            );
        }
        public override bool Equals(object obj)
        {
            return Equals(obj as IIndexKey);
        }
        public override int GetHashCode()
        {
            return String.Format("LanguageAndChannelScopedIndexKey-{0}-{1}-{2}", ProductKey, LanguageKey, ChannelKey).GetHashCode();
        }
        public override string ToString()
        {
            return String.Format("{0}:{1}-{2}-{3}", base.ToString(), ProductKey, LanguageKey, ChannelKey);
        }
    }
}
