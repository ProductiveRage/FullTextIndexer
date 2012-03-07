using System;
using System.Collections.Generic;

namespace Tester.KeyVariants
{
    /// <summary>
    /// As IIndexKey implement IEquality for IIndexKey, this class is straight-forward
    /// </summary>
    public class IndexKeyEqualityComparer : IEqualityComparer<IIndexKey>
    {
        public bool Equals(IIndexKey x, IIndexKey y)
        {
            if ((x == null) && (y == null))
                return true;
            else if ((x == null) || (y == null))
                return false;
            return x.Equals(y);
        }

        public int GetHashCode(IIndexKey obj)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            return obj.GetHashCode();
        }
    }
}
