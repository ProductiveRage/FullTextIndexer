using System;

namespace Querier.QuerySegments
{
	public abstract class ValueQuerySegment : IQuerySegment
	{
		public ValueQuerySegment(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
				throw new ArgumentException("Null/blank value specified");

			Value = value;
		}

		/// <summary>
		/// This will never be null or blank
		/// </summary>
		public string Value { get; private set;  }

		public override string ToString()
		{
			return base.ToString() + ":" + Value;
		}
	}
}
