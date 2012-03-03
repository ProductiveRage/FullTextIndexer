using System;
using System.Collections.Generic;

namespace Common.Lists
{
    [Serializable]
    public class NonNullImmutableList<T> : ImmutableList<T> where T : class
    {
        public NonNullImmutableList(IEnumerable<T> values, IValueValidator<T> validator) : base(values, new NonNullWrappingValueValidator<T>(validator)) { }

        public NonNullImmutableList(IEnumerable<T> values) : this(values, null) { }
        public NonNullImmutableList(IValueValidator<T> validator, params T[] values) : this((IEnumerable<T>)values, validator) { }
        public NonNullImmutableList(params T[] values) : this(null, values) { }
        public NonNullImmutableList() : this(new T[0]) { }

        public new NonNullImmutableList<T> Add(T value)
        {
            return toDerivedClass<NonNullImmutableList<T>>(base.Add(value));
        }
        public new NonNullImmutableList<T> AddRange(IEnumerable<T> values)
        {
            return toDerivedClass<NonNullImmutableList<T>>(base.AddRange(values));
        }
        public new NonNullImmutableList<T> Insert(int index, T value)
        {
            return toDerivedClass<NonNullImmutableList<T>>(base.Insert(index, value));
        }
        public new NonNullImmutableList<T> Remove(T value)
        {
            return toDerivedClass<NonNullImmutableList<T>>(base.Remove(value));
        }
		public new NonNullImmutableList<T> RemoveAt(int index)
		{
			return toDerivedClass<NonNullImmutableList<T>>(base.RemoveAt(index));
		}
		public new NonNullImmutableList<T> Sort(Comparison<T> comparison)
		{
			return toDerivedClass<NonNullImmutableList<T>>(base.Sort(comparison));
		}

        [Serializable]
		private class NonNullWrappingValueValidator<U> : IValueValidator<U> where U : class
        {
            private IValueValidator<U> validator;
            public NonNullWrappingValueValidator(IValueValidator<U> validator)
            {
                this.validator = validator;
            }
            public void EnsureValid(U value)
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                if (this.validator != null)
                    this.validator.EnsureValid(value);
            }
        }
    }
}
