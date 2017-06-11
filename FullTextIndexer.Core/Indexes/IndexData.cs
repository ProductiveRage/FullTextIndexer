using System;
using System.Collections.Generic;
using System.Linq;
using FullTextIndexer.Common.Lists;
using FullTextIndexer.Core.Indexes.TernarySearchTree;

namespace FullTextIndexer.Core.Indexes
{
#if NET45
	[Serializable]
#endif
	public class IndexData<TKey> : IIndexData<TKey>
	{
		private TernarySearchTreeDictionary<NonNullImmutableList<WeightedEntry<TKey>>> _data;
		private IEqualityComparer<TKey> _dataKeyComparer;
		public IndexData(
			TernarySearchTreeDictionary<NonNullImmutableList<WeightedEntry<TKey>>> data,
			IEqualityComparer<TKey> dataKeyComparer)
		{
			if (data == null)
				throw new ArgumentNullException("data");
			if (dataKeyComparer == null)
				throw new ArgumentNullException("dataKeyComparer");

			var allValues = data.GetAllValues();
			if (allValues.Any(v => v == null))
				throw new ArgumentException("data may not contain any null WeightedEntry list values");
			if (allValues.Any(v => v.Count == 0))
				throw new ArgumentException("data may not contain any empty WeightedEntry list values");

			_data = data;
			_dataKeyComparer = dataKeyComparer;
		}

		/// <summary>
		/// This will throw an exception for null or blank input. It will never return null. If there are no matches then an empty list will be returned.
		/// </summary>
		public NonNullImmutableList<WeightedEntry<TKey>> GetMatches(string source)
		{
			if (string.IsNullOrWhiteSpace(source))
				throw new ArgumentException("Null/blank source specified");

			// Since the data dictionary uses the sourceStringComparer, the lookup here will take that into account (so if a case-insensitive sourceStringComparer
			// was specified, for example, then the casing of the "source" value here will be irrelevant)
			NonNullImmutableList<WeightedEntry<TKey>> matches;
			if (!_data.TryGetValue(source, out matches))
				return new NonNullImmutableList<WeightedEntry<TKey>>();
			return matches;
		}

		/// <summary>
		/// This will return a new IndexData instance that combines the source instance's data with the data other IndexData instances using the specified weight combiner.
		/// In a case where there are different TokenComparer implementations on this instance and on dataToAdd, the comparer from the current instance will be used. It
		/// is recommended that a consistent TokenComparer be used at all times. An exception will be thrown for null dataToAdd or weightCombiner references.
		/// </summary>
		public IIndexData<TKey> Combine(NonNullImmutableList<IIndexData<TKey>> indexesToAdd, IndexGenerators.IndexGenerator.WeightedEntryCombiner weightCombiner)
		{
			if (indexesToAdd == null)
				throw new ArgumentNullException("indexesToAdd");
			if (weightCombiner == null)
				throw new ArgumentNullException("weightCombiner");

			// Start by taking a copy of the data in this instance, stored in a dictionary that we will add the other indexes' data to
			// - Note: This will inherit the keyNormaliser from this instance's TernarySearchTree
			var combinedContent = _data.ToDictionary();

			// Now combine the additional data
			foreach (var dictionaryDataToAdd in indexesToAdd.Select(i => i.ToDictionary()))
			{
				foreach (var entry in dictionaryDataToAdd)
				{
					if (!combinedContent.ContainsKey(entry.Key))
					{
						combinedContent.Add(entry.Key, entry.Value);
						continue;
					}

					// Note: There will never any WeightEntry lists in index data that are null or empty as there are constructor checks preventing this
					combinedContent[entry.Key] = combinedContent[entry.Key]
						.Concat(entry.Value)
						.GroupBy(weightedEntries => weightedEntries.Key, _dataKeyComparer)
						.Select(g => new WeightedEntry<TKey>(
							g.Key,
							weightCombiner(g.Select(e => e.Weight).ToImmutableList()),
							g.SelectMany(e => e.SourceLocations).ToNonNullImmutableList()
						))
						.ToNonNullImmutableList();
				}
			}

			// Return a new instance with this combined data
			return new IndexData<TKey>(
				new TernarySearchTreeDictionary<NonNullImmutableList<WeightedEntry<TKey>>>(combinedContent, _data.KeyNormaliser),
				_dataKeyComparer
			);
		}

		/// <summary>
		/// This will never return null, the returned dictionary will have this instance's KeyNormaliser as its comparer
		/// </summary>
		public IDictionary<string, NonNullImmutableList<WeightedEntry<TKey>>> ToDictionary()
		{
			return _data.ToDictionary();
		}

		/// <summary>
		/// This will return a new IndexData instance without any WeightedEntry values whose Keys match the removeIf predicate. If tokens are left without any WeightedEntry
		/// values then the token will be excluded from the new data. This will never return null. It will throw an exception for a null removeIf.
		/// </summary>
		public IIndexData<TKey> Remove(Predicate<TKey> removeIf)
		{
			if (removeIf == null)
				throw new ArgumentNullException("removeIf");

			var content = _data.ToDictionary();
			foreach (var token in content.Keys.ToArray()) // Take a copy of Keys since we may manipulate the dictionary content
			{
				var trimmedWeightedEntries = content[token].Where(e => !removeIf(e.Key));
				if (trimmedWeightedEntries.Any())
					content[token] = trimmedWeightedEntries.ToNonNullImmutableList();
				else
					content.Remove(token);
			}
			return new IndexData<TKey>(
				new TernarySearchTreeDictionary<NonNullImmutableList<WeightedEntry<TKey>>>(content, _data.KeyNormaliser),
				_dataKeyComparer
			);
		}

		/// <summary>
		/// This will never return null
		/// </summary>
		public NonNullOrEmptyStringList GetAllTokens()
		{
			return new NonNullOrEmptyStringList(_data.GetAllNormalisedKeys());
		}

		/// <summary>
		/// This will never return null
		/// </summary>
		public IStringNormaliser TokenComparer
		{
			get { return _data.KeyNormaliser; }
		}

		/// <summary>
		/// This will never return null
		/// </summary>
		public IEqualityComparer<TKey> KeyComparer
		{
			get { return _dataKeyComparer; }
		}
	}
}
