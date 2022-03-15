using System;
using System.Collections.Generic;
using FullTextIndexer.Common.Lists;
using FullTextIndexer.Core.Indexes;

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

		public TestIntKeyIndexDataBuilder Add(string source, int key, float weight, IEnumerable<SourceFieldLocation> sourceLocations)
		{
			if (string.IsNullOrWhiteSpace(source))
				throw new ArgumentException("Null/blank source specified");
			if (weight <= 0)
				throw new ArgumentOutOfRangeException(nameof(weight), "must be greater than zero");
			if (sourceLocations == null)
				throw new ArgumentNullException(nameof(sourceLocations));

			var newMatch = new WeightedEntry<int>(key, weight, sourceLocations.ToNonNullImmutableList());
			var newData = new Dictionary<string, NonNullImmutableList<WeightedEntry<int>>>(_data);
			if (!newData.ContainsKey(source))
				newData.Add(source, NonNullImmutableList<WeightedEntry<int>>.Empty);
			newData[source] = newData[source].Add(newMatch);
			return new TestIntKeyIndexDataBuilder(newData);
		}
		public TestIntKeyIndexDataBuilder Add(string source, int key, float weight, SourceFieldLocation sourceLocation)
		{
			if (sourceLocation == null)
				throw new ArgumentNullException(nameof(sourceLocation));

			return Add(source, key, weight, new[] { sourceLocation });
		}

		public TestIntKeyIndexData Get()
		{
			return new TestIntKeyIndexData(_data);
		}
	}
}