using System;
using System.Collections.Generic;

namespace FullTextIndexer.Core.IndexGenerators
{
    public class DefaultEqualityComparer<T> : IEqualityComparer<T>
    {
        public bool Equals(T x, T y)
        {
            if ((x == null) && (y == null))
                return true;
            else if ((x == null) || (y == null))
                return false;
            else
                return x.Equals(y);
        }

        public int GetHashCode(T obj)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");

            return obj.GetHashCode();
        }
    }
}
