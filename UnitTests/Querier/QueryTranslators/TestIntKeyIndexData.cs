using System;
using System.Collections.Generic;
using System.Linq;
using FullTextIndexer.Common.Lists;
using FullTextIndexer.Core.Indexes;
using FullTextIndexer.Core.Indexes.TernarySearchTree;
using FullTextIndexer.Core.IndexGenerators;

namespace UnitTests.Querier.QueryTranslators
{
	public class TestIntKeyIndexData : IIndexData<int>
	{
		private readonly Dictionary<string, NonNullImmutableList<WeightedEntry<int>>> _data;
		public TestIntKeyIndexData(Dictionary<string, NonNullImmutableList<WeightedEntry<int>>> data)
		{
			if (data == null)
				throw new ArgumentNullException("data");
			var dataCopy = new Dictionary<string, NonNullImmutableList<WeightedEntry<int>>>(data);
			if (dataCopy.Values.Any(v => v == null))
				throw new ArgumentException("Null Value encountered in data");

			_data = dataCopy;
		}

		/// <summary>
		/// This will throw an exception for null or blank input. It will never return null. If there are no matches then an empty list will be returned.
		/// </summary>
		public NonNullImmutableList<WeightedEntry<int>> GetMatches(string source)
		{
			if (string.IsNullOrWhiteSpace(source))
				throw new ArgumentException("Null/blank source specified");

			if (_data.ContainsKey(source))
				return _data[source];
			return new NonNullImmutableList<WeightedEntry<int>>();
		}

		/// <summary>
		/// This will never return null
		/// </summary>
		public IEqualityComparer<int> KeyComparer
		{
			get { return new DefaultEqualityComparer<int>(); }
		}

		// The following aren't required for testing and so aren't implemented..
		public IIndexData<int> Combine(NonNullImmutableList<IIndexData<int>> indexesToAdd, IndexGenerator.WeightedEntryCombiner weightCombiner)
		{
			throw new NotImplementedException();
		}
		public IIndexData<int> Add(IEnumerable<KeyValuePair<string, NonNullImmutableList<WeightedEntry<int>>>> data)
		{
			throw new NotImplementedException();
		}
		public IIndexData<int> Remove(Predicate<int> removeIf)
		{
			throw new NotImplementedException();
		}
		public IDictionary<string, NonNullImmutableList<WeightedEntry<int>>> ToDictionary()
		{
			throw new NotImplementedException();
		}
		public NonNullOrEmptyStringList GetAllTokens()
		{
			throw new NotImplementedException();
		}
		public IStringNormaliser TokenComparer
		{
			get { throw new NotImplementedException(); }
		}
	}
}
