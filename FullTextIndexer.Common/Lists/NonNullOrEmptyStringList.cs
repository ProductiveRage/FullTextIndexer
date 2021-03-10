using System;
using System.Collections.Generic;

namespace FullTextIndexer.Common.Lists
{
    [Serializable]
	public sealed class NonNullOrEmptyStringList : ImmutableList<string>
	{
		public static NonNullOrEmptyStringList Empty { get; } = new NonNullOrEmptyStringList(new string[0]);

		public NonNullOrEmptyStringList(IEnumerable<string> values) : base(values, Validator.Instance) { }
		public NonNullOrEmptyStringList(string value) : this(new Node { Value = value, Count = 1, Previous = null }) => Validator.Instance.EnsureValid(value);
		private NonNullOrEmptyStringList(Node tail) : base(tail, Validator.Instance) { }

		public new NonNullOrEmptyStringList Add(string value)
		{
			return ToNonNullOrEmptyStringList(base.Add(value));
		}
		public new NonNullOrEmptyStringList AddRange(IEnumerable<string> values)
		{
			return ToNonNullOrEmptyStringList(base.AddRange(values));
		}
		public new NonNullOrEmptyStringList Insert(string value, int insertAtIndex)
		{
			return ToNonNullOrEmptyStringList(base.Insert(value, insertAtIndex));
		}
		public new NonNullOrEmptyStringList Insert(IEnumerable<string> values, int insertAtIndex)
		{
			return ToNonNullOrEmptyStringList(base.Insert(values, insertAtIndex));
		}
		public new NonNullOrEmptyStringList Remove(string value)
		{
			return ToNonNullOrEmptyStringList(base.Remove(value));
		}
		public new NonNullOrEmptyStringList Remove(string value, IEqualityComparer<string> optionalComparer)
		{
			return ToNonNullOrEmptyStringList(base.Remove(value, optionalComparer));
		}
		public new NonNullOrEmptyStringList RemoveAt(int removeAtIndex)
		{
			return ToNonNullOrEmptyStringList(base.RemoveAt(removeAtIndex));
		}
		public new NonNullOrEmptyStringList RemoveRange(int removeAtIndex, int count)
		{
			return ToNonNullOrEmptyStringList(base.RemoveRange(removeAtIndex, count));
		}
		public new NonNullOrEmptyStringList Remove(Predicate<string> removeIf)
		{
			return ToNonNullOrEmptyStringList(base.Remove(removeIf));
		}
		public new NonNullOrEmptyStringList Sort()
		{
			return ToNonNullOrEmptyStringList(base.Sort());
		}
		public new NonNullOrEmptyStringList Sort(Comparison<string> optionalComparison)
		{
			return ToNonNullOrEmptyStringList(base.Sort(optionalComparison));
		}
		public new NonNullOrEmptyStringList Sort(IComparer<string> optionalComparer)
		{
			return ToNonNullOrEmptyStringList(base.Sort(optionalComparer));
		}

		private static NonNullOrEmptyStringList ToNonNullOrEmptyStringList(ImmutableList<string> list)
		{
			if (list == null)
				throw new ArgumentNullException("list");

			return To<NonNullOrEmptyStringList>(
				list,
				tail => new NonNullOrEmptyStringList(tail)
			);
		}

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