﻿using System;
using Common.Lists;

namespace FullTextIndexer.TokenBreaking
{
    public class WhiteSpaceTokenBreaker : ITokenBreaker
    {
        /// <summary>
        /// This will never return null. It will throw an exception for null input.
        /// </summary>
        public NonNullOrEmptyStringList Break(string value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            return new NonNullOrEmptyStringList(
                value.Split((char[])null, StringSplitOptions.RemoveEmptyEntries) // Passing (char[]) null will cause breaking on any whitespace char
            );
        }
    }
}
