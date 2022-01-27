using System;
using System.Collections.Generic;

namespace FullTextIndexer.Common.Lists
{
	public static class NonNullImmutableList
    {
		public static NonNullImmutableList<T> Create<T>(params T[] values) where T : class => new NonNullImmutableList<T>(values);
	}

	[Serializable]
	public sealed class NonNullImmutableList<T> : ImmutableList<T> where T : class
	{
		public static NonNullImmutableList<T> Empty { get; } = new NonNullImmutableList<T>(Array.Empty<T>());

		public NonNullImmutableList(IEnumerable<T> values) : base(values, Validator.Instance) { }
		public NonNullImmutableList(T value) : this(new Node { Value = value, Count = 1, Previous = null }) => Validator.Instance.EnsureValid(value);
		private NonNullImmutableList(Node tail) : base(tail, Validator.Instance) { }

		public new NonNullImmutableList<T> Add(T value) => ToNonNullImmutableList(base.Add(value));
		public new NonNullImmutableList<T> AddRange(IEnumerable<T> values) => ToNonNullImmutableList(base.AddRange(values));
		public new NonNullImmutableList<T> Insert(T value, int insertAtIndex) => ToNonNullImmutableList(base.Insert(value, insertAtIndex));
		public new NonNullImmutableList<T> Insert(IEnumerable<T> values, int insertAtIndex) => ToNonNullImmutableList(base.Insert(values, insertAtIndex));
		public new NonNullImmutableList<T> Remove(T value) => ToNonNullImmutableList(base.Remove(value));
		public new NonNullImmutableList<T> Remove(T value, IEqualityComparer<T> optionalComparer) => ToNonNullImmutableList(base.Remove(value, optionalComparer));
		public new NonNullImmutableList<T> RemoveAt(int removeAtIndex) => ToNonNullImmutableList(base.RemoveAt(removeAtIndex));
		public new NonNullImmutableList<T> RemoveRange(int removeAtIndex, int count) => ToNonNullImmutableList(base.RemoveRange(removeAtIndex, count));
		public new NonNullImmutableList<T> RemoveLast() => ToNonNullImmutableList(base.RemoveLast());
		public new NonNullImmutableList<T> RemoveLast(int numberToRemove) => ToNonNullImmutableList(base.RemoveLast(numberToRemove));
		public new NonNullImmutableList<T> Remove(Predicate<T> removeIf) => ToNonNullImmutableList(base.Remove(removeIf));
		public new NonNullImmutableList<T> Sort() => ToNonNullImmutableList(base.Sort());
		public new NonNullImmutableList<T> Sort(Comparison<T> optionalComparison) => ToNonNullImmutableList(base.Sort(optionalComparison));
		public new NonNullImmutableList<T> Sort(IComparer<T> optionalComparer) => ToNonNullImmutableList(base.Sort(optionalComparer));

		private NonNullImmutableList<T> ToNonNullImmutableList(ImmutableList<T> list) =>
			To(
				list ?? throw new ArgumentNullException(nameof(list)),
				tail => new NonNullImmutableList<T>(tail));

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