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
		private readonly TernarySearchTreeDictionary<NonNullImmutableList<WeightedEntry<TKey>>> _data;
		private readonly Lazy<bool> _sourceLocationsAvailable;
		public IndexData(TernarySearchTreeDictionary<NonNullImmutableList<WeightedEntry<TKey>>> data, IEqualityComparer<TKey> dataKeyComparer)
			: this(data, dataKeyComparer, validate: true) { }
		private IndexData(TernarySearchTreeDictionary<NonNullImmutableList<WeightedEntry<TKey>>> data, IEqualityComparer<TKey> dataKeyComparer, bool validate)
		{
			if (data == null)
				throw new ArgumentNullException("data");

			// If the constructor is called from a method within this class then the data should be known to be valid but if the constructor call was from
			// other code then perform some sanity checking on it
			if (validate)
			{
				var allValues = data.GetAllValues();
				if (allValues.Any(v => v == null))
					throw new ArgumentException("data may not contain any null WeightedEntry list values");
				if (allValues.Any(v => v.Count == 0))
					throw new ArgumentException("data may not contain any empty WeightedEntry list values");
			}

			_data = data;
			_sourceLocationsAvailable = new Lazy<bool>(() =>
			{
				var atLeastOneEntryIsMissingSourceLocations = _data.GetAllValues().Any(entries => entries.Any(entry => entry.SourceLocationsIfRecorded == null));
				return !atLeastOneEntryIsMissingSourceLocations;
			});
			KeyComparer = dataKeyComparer ?? throw new ArgumentNullException("dataKeyComparer");
		}

		/// <summary>
		/// If the Index was built such that source locations are available for every token match then this will return true (if the Index was constructed by an
		/// Index generator that did not record source locations or if this Index was created by combining other Indexes where at least one did not contain source
		/// locations then this will be false - recording source locations requires more memory but is essential for some functionality, such as the extension
		/// method GetConsecutiveMatches)
		/// </summary>
		public bool SourceLocationsAvailable { get { return _sourceLocationsAvailable.Value; } }

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
				return NonNullImmutableList<WeightedEntry<TKey>>.Empty;
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
						.GroupBy(weightedEntries => weightedEntries.Key, KeyComparer)
						.Select(g =>
						{
							if (g.Count() == 1)
								return g.First();

							return new WeightedEntry<TKey>(
								g.Key,
								weightCombiner(g.Select(e => e.Weight).ToImmutableList()),
								g.Any(e => e.SourceLocationsIfRecorded == null) ? null : g.SelectMany(e => e.SourceLocationsIfRecorded).ToNonNullImmutableList()
							);
						})
						.ToNonNullImmutableList();
				}
			}

			// Return a new instance with this combined data
			return new IndexData<TKey>(
				new TernarySearchTreeDictionary<NonNullImmutableList<WeightedEntry<TKey>>>(combinedContent, _data.KeyNormaliser),
				KeyComparer,
				validate: false
			);
		}

		/// <summary>
		/// This will return a new instance that combines the source instance's data with additional results - if results are being updated then the Remove method should
		/// be called first to ensure that duplicate match data entries are not present in the returned index. This will never return null. It will throw an exception for
		/// a null data reference or one that contains any null references.
		/// </summary>
		public IIndexData<TKey> Add(IEnumerable<KeyValuePair<string, NonNullImmutableList<WeightedEntry<TKey>>>> data)
		{
			if (data == null)
				throw new ArgumentNullException(nameof(data));

			var newIndex = _data.Add(
				data,
				(current, toAdd) => ((current != null) && (toAdd != null)) ? current.AddRange(toAdd) : (current ?? toAdd)
			);
			return new IndexData<TKey>(newIndex, KeyComparer, validate: false);
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

			return new IndexData<TKey>(
				_data.Update(weightedEntries =>
				{
					// Remove any weighted entries whose keys match the criteria - if this leaves no weighted entries then return null to indicate that the token no longer
					// has any data associated with it
					var trimmedWeightEntries = weightedEntries.Remove(weightedEntry => removeIf(weightedEntry.Key));
					return (trimmedWeightEntries.Count == 0) ? null : trimmedWeightEntries;
				}),
				KeyComparer,
				validate: false
			);
		}

		/// <summary>
		/// This will return a new IndexData instance without any WeightedEntry values whose Keys match thse in the keysToRemove list. If tokens are left without any
		/// WeightedEntry values then the token will be excluded from the new data. This will never return null. It will throw an exception for a null removeIf.
		/// </summary>
		public IIndexData<TKey> Remove(ImmutableList<TKey> keysToRemove)
		{
			if (keysToRemove == null)
				throw new ArgumentNullException("keysToRemove");

			var quickLookup = new HashSet<TKey>(keysToRemove, KeyComparer);
			return new IndexData<TKey>(
				_data.Update(weightedEntries =>
				{
					// Remove any weighted entries whose keys match the criteria - if this leaves no weighted entries then return null to indicate that the token no longer
					// has any data associated with it
					var trimmedWeightEntries = weightedEntries.Remove(weightedEntry => quickLookup.Contains(weightedEntry.Key));
					return (trimmedWeightEntries.Count == 0) ? null : trimmedWeightEntries;
				}),
				KeyComparer,
				validate: false
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
		public IEqualityComparer<TKey> KeyComparer { get; private set; }
	}
}
