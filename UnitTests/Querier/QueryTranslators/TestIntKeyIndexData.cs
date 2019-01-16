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
			return NonNullImmutableList<WeightedEntry<int>>.Empty;
		}

		/// <summary>
		/// If the Index was built such that source locations are available for every token match then this will return true (if the Index was constructed by an
		/// Index generator that did not record source locations or if this Index was created by combining other Indexes where at least one did not contain source
		/// locations then this will be false - recording source locations requires more memory but is essential for some functionality, such as the extension
		/// method GetConsecutiveMatches)
		/// </summary>
		public bool SourceLocationsAvailable
		{
			get
			{
				var atLeastOneEntryIsMissingSourceLocations = _data.Values.Any(entries => entries.Any(entry => entry.SourceLocationsIfRecorded == null));
				return !atLeastOneEntryIsMissingSourceLocations;
			}
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
		public IIndexData<int> Remove(ImmutableList<int> keysToRemove)
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