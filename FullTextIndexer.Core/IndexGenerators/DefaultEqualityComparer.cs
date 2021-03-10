using System;
using System.Collections.Generic;

namespace FullTextIndexer.Core.IndexGenerators
{
    /// <summary>
    /// The .NET framework has a builtin EqualityComparer&lt;T&gt;.Default option for cases where you want to compare two instances of a type without applying any special logic.
    /// However, that type is not decorated with the Serializable attribute and so you will have to be careful when trying to serialise an index that has a reference to one of
    /// those comparer (if you are using the IndexDataJsonSerialiser then that is not an issue and you may use either this or the framework class).
    /// </summary>
    [Serializable]
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
