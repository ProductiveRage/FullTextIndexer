﻿using System;
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
		private readonly ILogger _logger;
		public AutomatedIndexGeneratorFactory(
			Func<TSource, TKey> keyRetriever,
			IEqualityComparer<TKey> keyComparer,
			IStringNormaliser stringNormaliser,
			ITokenBreaker tokenBreaker,
			IndexGenerator.WeightedEntryCombiner weightedEntryCombiner,
			WeightDeterminerGenerator brokenTokenWeightDeterminerGenerator,
			PropertyInfo optionalPropertyForFirstContentRetriever,
			ILogger logger)
		{
			if (keyRetriever == null)
				throw new ArgumentNullException("keyRetriever");
			if (keyComparer == null)
				throw new ArgumentNullException("keyComparer");
			if (stringNormaliser == null)
				throw new ArgumentNullException("stringNormaliser");
			if (tokenBreaker == null)
				throw new ArgumentNullException("tokenBreaker");
			if (weightedEntryCombiner == null)
				throw new ArgumentNullException("weightedEntryCombiner");
			if (brokenTokenWeightDeterminerGenerator == null)
				throw new ArgumentNullException("brokenTokenWeightDeterminerGenerator");
			if (logger == null)
				throw new ArgumentNullException("logger");

			_keyRetriever = keyRetriever;
			_keyComparer = keyComparer;
			_stringNormaliser = stringNormaliser;
			_tokenBreaker = tokenBreaker;
			_weightedEntryCombiner = weightedEntryCombiner;
			_brokenTokenWeightDeterminerGenerator = brokenTokenWeightDeterminerGenerator;
			_optionalPropertyForFirstContentRetriever = optionalPropertyForFirstContentRetriever;
			_logger = logger;
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
				throw new ArgumentNullException("keyRetriever");
			if (nestedDataAccessor == null)
				throw new ArgumentNullException("dataRetriever");
			if (weightDeterminerGenerator == null)
				throw new ArgumentNullException("weightDeterminerGenerator");
			if (type == null)
				throw new ArgumentNullException("type");

			var propertyValueRetrievers = new NonNullImmutableList<ContentRetrieverWithSourceProperty>();
			foreach (var property in type.GetProperties().Where(p => p.CanRead && ((p.GetIndexParameters() ?? new ParameterInfo[0]).Length == 0)))
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
									var values = new NonNullOrEmptyStringList();
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
					propertyValueRetrievers = propertyValueRetrievers.AddRange(
						GenerateContentRetrievers(
							keyRetriever,
							nestedEnumerableTypeDataRetriever,
							weightDeterminerGenerator,
							nestedTypeElementType
						)
					);
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
				if (contentRetriever == null)
					throw new ArgumentNullException("contentRetriever");
				if (property == null)
					throw new ArgumentNullException("property");

				ContentRetriever = contentRetriever;
				Property = property;
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