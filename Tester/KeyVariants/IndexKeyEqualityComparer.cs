using System;

namespace Tester.KeyVariants
{
    public class NonScopedIndexKey : IIndexKey
    {
        public NonScopedIndexKey(int productKey)
        {
            ProductKey = productKey;
        }
        public int ProductKey { get; private set; }
        public bool IsApplicableFor(int languageKey, int channelKey)
        {
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
