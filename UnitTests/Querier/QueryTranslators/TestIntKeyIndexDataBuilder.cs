using System;
using System.Collections.Generic;
using Common.Lists;
using FullTextIndexer.Indexes;

namespace UnitTests.Querier.QueryTranslators
{
	public class TestIntKeyIndexDataBuilder
	{
		private readonly Dictionary<string, NonNullImmutableList<WeightedEntry<int>>> _data;
		public TestIntKeyIndexDataBuilder()
		{
			_data = new Dictionary<string, NonNullImmutableList<WeightedEntry<int>>>();
		}
		private TestIntKeyIndexDataBuilder(Dictionary<string, NonNullImmutableList<WeightedEntry<int>>> data)
		{
			_data = new Dictionary<string, NonNullImmutableList<WeightedEntry<int>>>(data);
		}

		public TestIntKeyIndexDataBuilder Add(string source, int key, float weight)
		{
			if (string.IsNullOrWhiteSpace(source))
				throw new ArgumentException("Null/blank source specified");
			if (weight <= 0)
				throw new ArgumentOutOfRangeException("weight", "must be greater than zero");

			var newMatch = new WeightedEntry<int>(key, weight);
			var newData = new Dictionary<string, NonNullImmutableList<WeightedEntry<int>>>(_data);
			if (!newData.ContainsKey(source))
				newData.Add(source, new NonNullImmutableList<WeightedEntry<int>>());
			newData[source] = newData[source].Add(newMatch);
			return new TestIntKeyIndexDataBuilder(newData);
		}

		public TestIntKeyIndexData Get()
		{
			return new TestIntKeyIndexData(_data);
		}
	}
}
