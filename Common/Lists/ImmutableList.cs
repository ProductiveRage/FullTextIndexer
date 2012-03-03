using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace Common.Lists
{
    [Serializable]
    public class ImmutableList<T> : IEnumerable<T>
    {
        private List<T> values;
        private IValueValidator<T> validator;
        public ImmutableList(IEnumerable<T> values, IValueValidator<T> validator)
        {
            if (values == null)
                throw new ArgumentNullException("values");

            var valuesList = new List<T>();
            foreach (var value in values)
            {
                if (validator != null)
                {
                    try { validator.EnsureValid(value); }
                    catch (Exception e)
                    {
                        throw new ArgumentException("Invalid reference encountered in values", e);
                    }
                }
                valuesList.Add(value);
            }
            this.values = valuesList;
            this.validator = validator;
        }
        public ImmutableList(IEnumerable<T> values) : this(values, null) { }
        public ImmutableList(IValueValidator<T> validator, params T[] values) : this((IEnumerable<T>)values, validator) { }
        public ImmutableList() : this(new T[0]) { }

        public T this[int index]
        {
            get
            {
                if ((index < 0) || (index >= this.values.Count))
                    throw new ArgumentOutOfRangeException("index");
                return this.values[index];
            }
        }

        public int Count
        {
            get { return this.values.Count; }
        }

        public bool Contains(T value)
        {
            return this.values.Contains(value);
        }

        public bool Contains(T value, IEqualityComparer<T> comparer)
        {
            if (comparer == null)
                throw new ArgumentNullException("comparer");
            return this.values.Any(v => comparer.Equals(v, value));
        }

        public ImmutableList<T> Add(T value)
        {
            return Insert(this.values.Count, value);
        }

        public ImmutableList<T> AddRange(IEnumerable<T> values)
        {
            if (values == null)
                throw new ArgumentNullException("values");

            var valuesNew = new List<T>();
            valuesNew.AddRange(this.values);
            foreach (var value in values)
            {
                if (this.validator != null)
                {
                    try { this.validator.EnsureValid(value); }
                    catch (Exception e)
                    {
                        throw new ArgumentException("Invalid value", e);
                    }
                }
                valuesNew.Add(value);
            }
            return new ImmutableList<T>()
            {
                values = valuesNew,
                validator = this.validator
            };
        }

        public ImmutableList<T> AddRange(params T[] values)
        {
            return AddRange((IEnumerable<T>)values);
        }

        public ImmutableList<T> Insert(int index, T value)
        {
            if ((index < 0) || (index > this.values.Count))
                throw new ArgumentOutOfRangeException("index");
            if (this.validator != null)
            {
                try { this.validator.EnsureValid(value); }
                catch (Exception e)
                {
                    throw new ArgumentException("Invalid value", e);
                }
            }
            var valuesNew = new List<T>();
            valuesNew.AddRange(this.values);
            valuesNew.Insert(index, value);
            return new ImmutableList<T>()
            {
                values = valuesNew,
                validator = this.validator
            };
        }

        /// <summary>
        /// Removes the first occurrence of a specific object
        /// </summary>
        public ImmutableList<T> Remove(T value)
        {
            var valuesNew = new List<T>();
            valuesNew.AddRange(this.values);
            valuesNew.Remove(value);
            return new ImmutableList<T>()
            {
                values = valuesNew,
                validator = this.validator
            };
        }

        public ImmutableList<T> RemoveAt(int index)
        {
            if ((index < 0) || (index >= this.values.Count))
                throw new ArgumentOutOfRangeException("index");
            var valuesNew = new List<T>();
            valuesNew.AddRange(this.values);
            valuesNew.RemoveAt(index);
            return new ImmutableList<T>()
            {
                values = valuesNew,
                validator = this.validator
            };
        }

		public ImmutableList<T> Sort(Comparison<T> comparison)
		{
			if (comparison == null)
				throw new ArgumentNullException("comparison");
            var valuesNew = new List<T>();
            valuesNew.AddRange(this.values);
			valuesNew.Sort(comparison);
			return valuesNew.ToImmutableList();
		}

        /// <summary>
        /// This is just a convenience method so that derived types can call Add, Remove, etc.. and return instances of themselves without having to
        /// pass that data back through a constructor which will check each value against the validator even though we already know they're valid!
        /// Note: This can only be used by derived classes that don't have any new requirements of any type - we're setting only the values and
        /// validator references here!
        /// </summary>
        protected static U toDerivedClass<U>(ImmutableList<T> list) where U : ImmutableList<T>, new()
        {
            if (list == null)
                throw new ArgumentNullException("list");

            // Use same trick as above methods to cheat - we're changing the state of the object after instantiation, but after returning from
            // this method it can be considered immutable
            return new U()
            {
                values = list.values,
                validator = list.validator
            };
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this.values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
