using System;
using System.Collections.Generic;

namespace Common.Lists
{
    [Serializable]
    public class NonNullOrEmptyStringList : ImmutableList<string>
    {
        public NonNullOrEmptyStringList(IEnumerable<string> values) : base(values, new NonNullOrEmptyStringValidator()) { }
        public NonNullOrEmptyStringList() : this(new string[0]) { }

		public new NonNullOrEmptyStringList Add(string value)
        {
            return toDerivedClass<NonNullOrEmptyStringList>(base.Add(value));
        }
		public new NonNullOrEmptyStringList AddRange(IEnumerable<string> values)
        {
            return toDerivedClass<NonNullOrEmptyStringList>(base.AddRange(values));
        }
		public new NonNullOrEmptyStringList Insert(int index, string value)
        {
            return toDerivedClass<NonNullOrEmptyStringList>(base.Insert(index, value));
        }
		public new NonNullOrEmptyStringList Remove(string value)
        {
            return toDerivedClass<NonNullOrEmptyStringList>(base.Remove(value));
        }
        public new NonNullOrEmptyStringList RemoveAt(int index)
        {
            return toDerivedClass<NonNullOrEmptyStringList>(base.RemoveAt(index));
        }
		public new NonNullOrEmptyStringList Sort(Comparison<string> comparison)
		{
			return toDerivedClass<NonNullOrEmptyStringList>(base.Sort(comparison));
		}

		[Serializable]
		private class NonNullOrEmptyStringValidator : IValueValidator<string>
        {
            public void EnsureValid(string value)
            {
				if (string.IsNullOrWhiteSpace(value))
					throw new ArgumentException("Null or empty value specified");
            }
        }
    }
}
