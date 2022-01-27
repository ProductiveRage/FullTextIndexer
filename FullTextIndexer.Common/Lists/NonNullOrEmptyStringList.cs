using System;
using System.Collections.Generic;

namespace FullTextIndexer.Common.Lists
{
    [Serializable]
	public sealed class NonNullOrEmptyStringList : ImmutableList<string>
	{
		public static NonNullOrEmptyStringList Empty { get; } = new NonNullOrEmptyStringList(Array.Empty<string>());

		public NonNullOrEmptyStringList(IEnumerable<string> values) : base(values, Validator.Instance) { }
		public NonNullOrEmptyStringList(string value) : this(new Node { Value = value, Count = 1, Previous = null }) => Validator.Instance.EnsureValid(value);
		private NonNullOrEmptyStringList(Node tail) : base(tail, Validator.Instance) { }

		public new NonNullOrEmptyStringList Add(string value) => ToNonNullOrEmptyStringList(base.Add(value));
		public new NonNullOrEmptyStringList AddRange(IEnumerable<string> values) => ToNonNullOrEmptyStringList(base.AddRange(values));
		public new NonNullOrEmptyStringList Insert(string value, int insertAtIndex) => ToNonNullOrEmptyStringList(base.Insert(value, insertAtIndex));
		public new NonNullOrEmptyStringList Insert(IEnumerable<string> values, int insertAtIndex) => ToNonNullOrEmptyStringList(base.Insert(values, insertAtIndex));
		public new NonNullOrEmptyStringList Remove(string value) => ToNonNullOrEmptyStringList(base.Remove(value));
		public new NonNullOrEmptyStringList Remove(string value, IEqualityComparer<string> optionalComparer) => ToNonNullOrEmptyStringList(base.Remove(value, optionalComparer));
		public new NonNullOrEmptyStringList RemoveAt(int removeAtIndex) => ToNonNullOrEmptyStringList(base.RemoveAt(removeAtIndex));
		public new NonNullOrEmptyStringList RemoveRange(int removeAtIndex, int count) => ToNonNullOrEmptyStringList(base.RemoveRange(removeAtIndex, count));
		public new NonNullOrEmptyStringList RemoveLast() => ToNonNullOrEmptyStringList(base.RemoveLast());
		public new NonNullOrEmptyStringList RemoveLast(int numberToRemove) => ToNonNullOrEmptyStringList(base.RemoveLast(numberToRemove));
		public new NonNullOrEmptyStringList Remove(Predicate<string> removeIf) => ToNonNullOrEmptyStringList(base.Remove(removeIf));
		public new NonNullOrEmptyStringList Sort() => ToNonNullOrEmptyStringList(base.Sort());
		public new NonNullOrEmptyStringList Sort(Comparison<string> optionalComparison) => ToNonNullOrEmptyStringList(base.Sort(optionalComparison));
		public new NonNullOrEmptyStringList Sort(IComparer<string> optionalComparer) => ToNonNullOrEmptyStringList(base.Sort(optionalComparer));

		private static NonNullOrEmptyStringList ToNonNullOrEmptyStringList(ImmutableList<string> list) =>
			To(
				list ?? throw new ArgumentNullException(nameof(list)),
				tail => new NonNullOrEmptyStringList(tail));

	    [Serializable]
		private sealed class Validator : IValueValidator<string>
		{
			public static Validator Instance { get; } = new Validator();
			private Validator() { }

			/// <summary>
			/// This will throw an exception for a value that does pass validation requirements
			/// </summary>
			public void EnsureValid(string value)
			{
				if (string.IsNullOrWhiteSpace(value))
					throw new ArgumentException($"Null/blank {nameof(value)} specified");
			}
		}
	}
}