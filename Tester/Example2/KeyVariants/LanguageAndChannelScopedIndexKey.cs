using System;
using Tester.Example2.SourceData;

namespace Tester.Example2.KeyVariants
{
    public sealed class LanguageAndChannelScopedIndexKey : IIndexKey
    {
        public LanguageAndChannelScopedIndexKey(int productKey, LanguageDetails language, int channelKey)
        {
            if (language == null)
                throw new ArgumentNullException("language");

            ProductKey = productKey;
            Language = language;
            ChannelKey = channelKey;
        }

        public int ProductKey { get; private set; }

        public LanguageDetails Language { get; private set; }

        public int ChannelKey { get; private set; }

        /// <summary>
        /// This will throw an exception for a null language reference
        /// </summary>
        public bool IsApplicableFor(LanguageDetails language, int channelKey)
        {
            if (language == null)
                throw new ArgumentNullException("language");

            return (language.Equals(Language) && (channelKey == ChannelKey));
        }

        public bool Equals(IIndexKey obj)
        {
            var objLanguageAndChannelScopedIndexKey = obj as LanguageAndChannelScopedIndexKey;
            if (objLanguageAndChannelScopedIndexKey == null)
                return false;

            return (
                (objLanguageAndChannelScopedIndexKey.ProductKey == ProductKey) &&
                (objLanguageAndChannelScopedIndexKey.Language.Equals(Language.Key)) &&
                (objLanguageAndChannelScopedIndexKey.ChannelKey == ChannelKey)
            );
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
            return String.Format("{0}:{1}-{2}-{3}", base.ToString(), ProductKey, Language.Key, ChannelKey);
        }
    }
}
