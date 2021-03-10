using System;
using System.Collections.Generic;

namespace FullTextIndexer.Common.Lists
{
    [Serializable]
	public sealed class NonNullImmutableList<T> : ImmutableList<T> where T : class
	{
		public static NonNullImmutableList<T> Empty { get; } = new NonNullImmutableList<T>(new T[0]);

		public NonNullImmutableList(IEnumerable<T> values) : base(values, Validator.Instance) { }
		public NonNullImmutableList(T value) : this(new Node { Value = value, Count = 1, Previous = null }) => Validator.Instance.EnsureValid(value);
		private NonNullImmutableList(Node tail) : base(tail, Validator.Instance) { }

		public new NonNullImmutableList<T> Add(T value)
		{
			return ToNonNullImmutableList(base.Add(value));
		}
		public new NonNullImmutableList<T> AddRange(IEnumerable<T> values)
		{
			return ToNonNullImmutableList(base.AddRange(values));
		}
		public new NonNullImmutableList<T> Insert(T value, int insertAtIndex)
		{
			return ToNonNullImmutableList(base.Insert(value, insertAtIndex));
		}
		public new NonNullImmutableList<T> Insert(IEnumerable<T> values, int insertAtIndex)
		{
			return ToNonNullImmutableList(base.Insert(values, insertAtIndex));
		}
		public new NonNullImmutableList<T> Remove(T value)
		{
			return ToNonNullImmutableList(base.Remove(value));
		}
		public new NonNullImmutableList<T> Remove(T value, IEqualityComparer<T> optionalComparer)
		{
			return ToNonNullImmutableList(base.Remove(value, optionalComparer));
		}
		public new NonNullImmutableList<T> RemoveAt(int removeAtIndex)
		{
			return ToNonNullImmutableList(base.RemoveAt(removeAtIndex));
		}
		public new NonNullImmutableList<T> RemoveRange(int removeAtIndex, int count)
		{
			return ToNonNullImmutableList(base.RemoveRange(removeAtIndex, count));
		}
		public new NonNullImmutableList<T> RemoveLast()
		{
			return ToNonNullImmutableList(base.RemoveLast());
		}
		public new NonNullImmutableList<T> RemoveLast(int numberToRemove)
		{
			return ToNonNullImmutableList(base.RemoveLast(numberToRemove));
		}
		public new NonNullImmutableList<T> Remove(Predicate<T> removeIf)
		{
			return ToNonNullImmutableList(base.Remove(removeIf));
		}
		public new NonNullImmutableList<T> Sort()
		{
			return ToNonNullImmutableList(base.Sort());
		}
		public new NonNullImmutableList<T> Sort(Comparison<T> optionalComparison)
		{
			return ToNonNullImmutableList(base.Sort(optionalComparison));
		}
		public new NonNullImmutableList<T> Sort(IComparer<T> optionalComparer)
		{
			return ToNonNullImmutableList(base.Sort(optionalComparer));
		}
		private NonNullImmutableList<T> ToNonNullImmutableList(ImmutableList<T> list)
		{
			if (list == null)
				throw new ArgumentNullException("list");

			return To<NonNullImmutableList<T>>(
				list,
				tail => new NonNullImmutableList<T>(tail)
			);
		}

	    [Serializable]
		private sealed class Validator : IValueValidator<T>
		{
			public static Validator Instance { get; } = new Validator();
			private Validator() { }

			/// <summary>
			/// This will throw an exception for a value that does pass validation requirements
			/// </summary>
			public void EnsureValid(T value)
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));
			}
		}
	}
}