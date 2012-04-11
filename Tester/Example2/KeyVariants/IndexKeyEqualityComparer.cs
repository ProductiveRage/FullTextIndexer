using System;
using Tester.Example2.SourceData;

namespace Tester.Example2.KeyVariants
{
    public sealed class NonScopedIndexKey : IIndexKey
    {
        public NonScopedIndexKey(int productKey)
        {
            ProductKey = productKey;
        }

        public int ProductKey { get; private set; }

        /// <summary>
        /// This will throw an exception for a null language reference
        /// </summary>
        public bool IsApplicableFor(LanguageDetails language, int channelKey)
        {
            if (language == null)
                throw new ArgumentNullException("language");

            return true; // This isn't scoped to any specific language or channel so it's always applicable
        }

        public bool Equals(IIndexKey obj)
        {
            if (obj == null || (obj.GetType() != GetType()))
                return false;
            return (obj.ProductKey == ProductKey);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as IIndexKey);
        }

        public override int GetHashCode()
        {
            return String.Format("NonScopedIndexKey-{0}", ProductKey).GetHashCode();
        }

        public override string ToString()
        {
            return String.Format("{0}:{1}", base.ToString(), ProductKey);
        }
    }
}
