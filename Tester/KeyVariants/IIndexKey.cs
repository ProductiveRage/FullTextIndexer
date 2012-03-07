using System;

namespace Tester.KeyVariants
{
    public interface IIndexKey : IEquatable<IIndexKey>
    {
        int ProductKey { get; }
        bool IsApplicableFor(int languageKey, int channelKey);
        new bool Equals(IIndexKey obj);
    }
}
