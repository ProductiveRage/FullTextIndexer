﻿using System;

namespace FullTextIndexer.Core.Indexes.TernarySearchTree
{
    /// <summary>
    /// This base class contains simple Equals and GetHashCode implementations which all IStringNormaliser classes must have
    /// </summary>
    [Serializable]
	public abstract class StringNormaliser : IStringNormaliser
    {
        /// <summary>
        /// This will never return null. If it returns an empty string then the string should not be considered elligible as a key. It will throw
        /// an exception for a null value.
        /// </summary>
        public abstract string GetNormalisedString(string value);

        public bool Equals(string x, string y)
        {
            if (x == null)
                throw new ArgumentNullException(nameof(x));
            if (y == null)
                throw new ArgumentNullException(nameof(y));

            return GetNormalisedString(x) == GetNormalisedString(y);
        }

        public int GetHashCode(string obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            return GetNormalisedString(obj).GetHashCode();
        }
    }
}
