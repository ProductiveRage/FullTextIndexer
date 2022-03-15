using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FullTextIndexer.Common.Lists;
using FullTextIndexer.Common.Logging;
using FullTextIndexer.Core.Indexes.TernarySearchTree;
using FullTextIndexer.Core.IndexGenerators;
using FullTextIndexer.Core.TokenBreaking;

namespace FullTextIndexer.Helpers
{
	/// <summary>
	/// This will try to automatically construct an Index Generator for a particular type (TSource). The type of the key for the source type must be specified (TKey) along with a
	/// way to retrieve the key from a source type instance (the keyRetriever) and a way to adjust match weights based upon the source properties (if a property shouldn't have its
	/// data contribute to the index data then the specified WeightDeterminerGenerator may return a null BrokenTokenWeightDeterminer instance for that property).
	/// </summary>
	public class AutomatedIndexGeneratorFactory<TSource, TKey> where TSource : class
	{
		private readonly Func<TSource, TKey> _keyRetriever;
		private readonly IEqualityComparer<TKey> _keyComparer;
		private readonly IStringNormaliser _stringNormaliser;
		private readonly ITokenBreaker _tokenBreaker;
		private readonly IndexGenerator.WeightedEntryCombiner _weightedEntryCombiner;
		private readonly WeightDeterminerGenerator _brokenTokenWeightDeterminerGenerator;
		private readonly PropertyInfo _optionalPropertyForFirstContentRetriever;
		private readonly bool _captureSourceLocations;
		private readonly ILogger _logger;
		public AutomatedIndexGeneratorFactory(
			Func<TSource, TKey> keyRetriever,
			IEqualityComparer<TKey> keyComparer,
			IStringNormaliser stringNormaliser,
			ITokenBreaker tokenBreaker,
			IndexGenerator.WeightedEntryCombiner weightedEntryCombiner,
			WeightDeterminerGenerator brokenTokenWeightDeterminerGenerator,
			PropertyInfo optionalPropertyForFirstContentRetriever,
			bool captureSourceLocations,
			ILogger logger)
		{
			_keyRetriever = keyRetriever ?? throw new ArgumentNullException(nameof(keyRetriever));
			_keyComparer = keyComparer ?? throw new ArgumentNullException(nameof(keyComparer));
			_stringNormaliser = stringNormaliser ?? throw new ArgumentNullException(nameof(stringNormaliser));
			_tokenBreaker = tokenBreaker ?? throw new ArgumentNullException(nameof(tokenBreaker));
			_weightedEntryCombiner = weightedEntryCombiner ?? throw new ArgumentNullException(nameof(weightedEntryCombiner));
			_brokenTokenWeightDeterminerGenerator = brokenTokenWeightDeterminerGenerator ?? throw new ArgumentNullException(nameof(brokenTokenWeightDeterminerGenerator));
			_optionalPropertyForFirstContentRetriever = optionalPropertyForFirstContentRetriever;
			_captureSourceLocations = captureSourceLocations;
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <summary>
		/// This will never be called with a null property reference. If it returns null then the property will be excluded from the final data.
		/// </summary>
		public delegate ContentRetriever<TSource, TKey>.BrokenTokenWeightDeterminer WeightDeterminerGenerator(PropertyInfo property);

		public IIndexGenerator<TSource, TKey> Get()
		{
			var contentRetrievers = GenerateContentRetrievers(
				_keyRetriever,
				source => new[] { source },
				_brokenTokenWeightDeterminerGenerator,
				typeof(TSource)
			);
			if (_optionalPropertyForFirstContentRetriever != null)
			{
				contentRetrievers = contentRetrievers.Sort((x, y) =>
				{
					var xIsFromFirstContentRetrieverProperty = (x.Property == _optionalPropertyForFirstContentRetriever);
					var yIsFromFirstContentRetrieverProperty = (y.Property == _optionalPropertyForFirstContentRetriever);
					if (xIsFromFirstContentRetrieverProperty && !yIsFromFirstContentRetrieverProperty)
						return -1;
					else if (!xIsFromFirstContentRetrieverProperty && yIsFromFirstContentRetrieverProperty)
						return 1;
					else
						return 0;
				});
			}

			return new IndexGenerator<TSource, TKey>(
				contentRetrievers.Select(c => c.ContentRetriever).ToNonNullImmutableList(),
				_keyComparer,
				_stringNormaliser,
				_tokenBreaker,
				_weightedEntryCombiner,
				_captureSourceLocations,
				_logger
			);
		}

		/// <summary>
		/// All of the values returned by the nestedDataAccessor must be of type "type" (or assignable to it)
		/// </summary>
		private NonNullImmutableList<ContentRetrieverWithSourceProperty> GenerateContentRetrievers(
			Func<TSource, TKey> keyRetriever,
			Func<TSource, IEnumerable> nestedDataAccessor,
			WeightDeterminerGenerator weightDeterminerGenerator,
			Type type)
		{
			if (keyRetriever == null)
				throw new ArgumentNullException(nameof(keyRetriever));
			if (nestedDataAccessor == null)
				throw new ArgumentNullException("dataRetriever");
			if (weightDeterminerGenerator == null)
				throw new ArgumentNullException(nameof(weightDeterminerGenerator));
			if (type == null)
				throw new ArgumentNullException(nameof(type));

			// 2018-03-07 DWR: When looking for properties to interrogate, the easy checks are CanRead (need to be able to read the value to extract content!) and ensuring that it is
			// not an index property (what index value(s) would we pass whenever we wanted to extract content?) but I also want to consider only instance properties because there is
			// HOPEFULLY not any value in looking at static properties because these won't vary from instance to instance (if there are derived types of TSource then there could
			// arguably be different static property values per type but I'm not worried about that enough to offset the benefit of ignoring static properties) and there is a fairly
			// common pattern that breaks if we try to consider static properties where an immutable type may have an "Empty" or "Default" (or similar) property that is the initial
			// state that should be used in favour of calling the constructor (so that instances of that initial state are shared) - if we try to examine that property then we'll
			// end up in an infinite loop here.
			var propertiesToConsider = type
				.GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.Where(p => p.CanRead && ((p.GetIndexParameters() ?? Array.Empty<ParameterInfo>()).Length == 0));

			var propertyValueRetrievers = NonNullImmutableList<ContentRetrieverWithSourceProperty>.Empty;
			foreach (var property in propertiesToConsider)
			{
				var weightDeterminer = weightDeterminerGenerator(property);
				if (weightDeterminer == null)
					continue;

				if (property.PropertyType == typeof(string))
				{
					var propertyClone = property; // Take a copy of the reference to use in the closure
					propertyValueRetrievers = propertyValueRetrievers.Add(
						new ContentRetrieverWithSourceProperty(
							new ContentRetriever<TSource, TKey>(
								source =>
								{
									var values = NonNullOrEmptyStringList.Empty;
									foreach (var entry in nestedDataAccessor(source))
									{
										if (entry == null)
											continue;

										var value = (string)propertyClone.GetValue(entry, null);
										if (!string.IsNullOrWhiteSpace(value))
											values = values.Add(value);
									}
									return new PreBrokenContent<TKey>(
										keyRetriever(source),
										values
									);
								},
								weightDeterminer
							),
							property
						)
					);
					continue;
				}

				if (typeof(IEnumerable).IsAssignableFrom(property.PropertyType))
				{
					Type nestedTypeElementType;
					if (property.PropertyType.IsArray)
					{
						if (property.PropertyType.GetArrayRank() != 1)
						{
							_logger.LogIgnoringAnyError(LogLevel.Warning, () => "Currently one single-dimensional arrays are supported, ignoring: " + type.FullName + "." + property);
							continue;
						}
						nestedTypeElementType = property.PropertyType.GetElementType();
					}
					else
					{
						var genericTypeParams = property.PropertyType.GetGenericArguments();
						if ((genericTypeParams == null) || (genericTypeParams.Length != 1))
							continue;
						nestedTypeElementType = genericTypeParams[0];
					}

					var propertyClone = property; // Take a copy of the reference to use in the closure
					Func<TSource, IEnumerable> nestedEnumerableTypeDataRetriever = source =>
					{
						var nestedTypeValues = new List<object>();
						foreach (var entry in nestedDataAccessor(source))
						{
							var nestedTypeEnumerableValue = (IEnumerable)propertyClone.GetValue(entry, null);
							if (nestedTypeEnumerableValue == null)
								continue;

							foreach (var enumeratedEntry in nestedTypeEnumerableValue)
							{
								if (enumeratedEntry == null)
									continue;

								nestedTypeValues.Add(enumeratedEntry);
							}
						}
						return nestedTypeValues;
					};
					if (nestedTypeElementType == typeof(string))
					{
						propertyValueRetrievers = propertyValueRetrievers.Add(
							new ContentRetrieverWithSourceProperty(
								new ContentRetriever<TSource, TKey>(
									source =>
									{
										var values = NonNullOrEmptyStringList.Empty;
										var nestedTypeEnumerableValue = nestedEnumerableTypeDataRetriever(source);
										if (nestedTypeEnumerableValue != null)
										{
											foreach (var entry in nestedTypeEnumerableValue)
											{
												var stringValue = entry as string;
												if (!string.IsNullOrWhiteSpace(stringValue))
													values = values.Add(stringValue);
											}
										}
										return new PreBrokenContent<TKey>(
											keyRetriever(source),
											values
										);
									},
									weightDeterminer
								),
								property
							)
						);
					}
					else
					{
						propertyValueRetrievers = propertyValueRetrievers.AddRange(
							GenerateContentRetrievers(
								keyRetriever,
								nestedEnumerableTypeDataRetriever,
								weightDeterminerGenerator,
								nestedTypeElementType
							)
						);
					}
					continue;
				}

				if (type != typeof(object))
				{
					var propertyClone = property; // Take a copy of the reference to use in the closure
					Func<TSource, IEnumerable> nestedEnumerableTypeDataRetriever = source =>
					{
						var nestedTypeValues = new List<object>();
						foreach (var entry in nestedDataAccessor(source))
						{
							var entryValue = propertyClone.GetValue(entry, null);
							if (entryValue != null)
								nestedTypeValues.Add(entryValue);
						}
						return nestedTypeValues;
					};
					propertyValueRetrievers = propertyValueRetrievers.AddRange(
						GenerateContentRetrievers(
							keyRetriever,
							nestedEnumerableTypeDataRetriever,
							weightDeterminerGenerator,
							property.PropertyType
						)
					);
					continue;
				}
			}
			return propertyValueRetrievers;
		}

		private class ContentRetrieverWithSourceProperty
		{
			public ContentRetrieverWithSourceProperty(ContentRetriever<TSource, TKey> contentRetriever, PropertyInfo property)
			{
				ContentRetriever = contentRetriever ?? throw new ArgumentNullException(nameof(contentRetriever));
				Property = property ?? throw new ArgumentNullException(nameof(property));
			}

			/// <summary>
			/// This will never be null
			/// </summary>
			public ContentRetriever<TSource, TKey> ContentRetriever { get; private set; }

			/// <summary>
			/// This will never be null
			/// </summary>
			public PropertyInfo Property { get; private set; }
		}
	}
}