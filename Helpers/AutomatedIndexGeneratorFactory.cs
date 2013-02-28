using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using FullTextIndexer.Common.Lists;
using FullTextIndexer.Common.Logging;
using FullTextIndexer.Core.Indexes.TernarySearchTree;
using FullTextIndexer.Core.IndexGenerators;
using FullTextIndexer.Core.TokenBreaking;

namespace FullTextIndexer.Helpers
{
	/// <summary>
	/// This will try to automatically construct an Index Generator for a particular type (TSource). The type of the key for the source type must be specified (TKey) along with a
	/// way to retrieve the key from a source type instance (the keyRetriever) and a way to specify which properties should and shouldn't be considered when the object graph is
	/// traversed (the propertyFilter). All of the textual content encountered will be combined into one field so it is not possible to apply different weights to different
	/// properties.
	/// </summary>
	public class AutomatedIndexGeneratorFactory<TSource, TKey> where TSource : class
	{
		private readonly Func<TSource, TKey> _keyRetriever;
		private readonly IEqualityComparer<TKey> _keyComparer;
		private readonly IStringNormaliser _stringNormaliser;
		private readonly ITokenBreaker _tokenBreaker;
		private readonly IndexGenerator.WeightedEntryCombiner _weightedEntryCombiner;
		private readonly ContentRetriever<TSource, TKey>.BrokenTokenWeightDeterminer _brokenTokenWeightDeterminer;
		private readonly Predicate<PropertyInfo> _propertyFilter;
		private readonly ILogger _logger;
		public AutomatedIndexGeneratorFactory(
			Func<TSource, TKey> keyRetriever,
			Predicate<PropertyInfo> propertyFilter,
			IEqualityComparer<TKey> keyComparer,
			IStringNormaliser stringNormaliser,
			ITokenBreaker tokenBreaker,
			IndexGenerator.WeightedEntryCombiner weightedEntryCombiner,
			ContentRetriever<TSource, TKey>.BrokenTokenWeightDeterminer brokenTokenWeightDeterminer,
			ILogger logger)
		{
			if (keyRetriever == null)
				throw new ArgumentNullException("keyRetriever");
			if (propertyFilter == null)
				throw new ArgumentNullException("propertyFilter");
			if (keyComparer == null)
				throw new ArgumentNullException("keyComparer");
			if (stringNormaliser == null)
				throw new ArgumentNullException("stringNormaliser");
			if (tokenBreaker == null)
				throw new ArgumentNullException("tokenBreaker");
			if (weightedEntryCombiner == null)
				throw new ArgumentNullException("weightedEntryCombiner");
			if (brokenTokenWeightDeterminer == null)
				throw new ArgumentNullException("brokenTokenWeightDeterminer");
			if (logger == null)
				throw new ArgumentNullException("logger");

			_keyRetriever = keyRetriever;
			_propertyFilter = propertyFilter;
			_keyComparer = keyComparer;
			_stringNormaliser = stringNormaliser;
			_tokenBreaker = tokenBreaker;
			_weightedEntryCombiner = weightedEntryCombiner;
			_brokenTokenWeightDeterminer = brokenTokenWeightDeterminer;
			_logger = logger;
		}

		public IIndexGenerator<TSource, TKey> Get()
		{
			var contentRetrievers = new[]
			{
				new ContentRetriever<TSource, TKey>(
					source =>
					{
						var contentBuilder = new StringBuilder();
						foreach (var contentSection in GetContent(source))
							contentBuilder.AppendLine(contentSection);
						return new PreBrokenContent<TKey>(
							_keyRetriever(source),
							contentBuilder.ToString()
						);
					},
					token => _brokenTokenWeightDeterminer(token)
				)
			};
			return new IndexGenerator<TSource, TKey>(
				contentRetrievers.ToNonNullImmutableList(),
				_keyComparer,
				_stringNormaliser,
				_tokenBreaker,
				_weightedEntryCombiner,
				_logger
			);
		}

		private NonNullOrEmptyStringList GetContent(object source)
		{
			if (source == null)
				throw new ArgumentNullException("source");

			var contentSections = new NonNullOrEmptyStringList();
			foreach (var property in source.GetType().GetProperties().Where(p => p.CanRead && ((p.GetIndexParameters() ?? new ParameterInfo[0]).Length == 0)))
			{
				if (!_propertyFilter(property))
					continue;

				if (property.PropertyType == typeof(string))
				{
					var stringValue = (string)property.GetValue(source, null);
					if (!string.IsNullOrWhiteSpace(stringValue))
						contentSections = contentSections.Add(stringValue);
					continue;
				}

				if (typeof(IEnumerable).IsAssignableFrom(property.PropertyType))
				{
					var enumerable = (IEnumerable)property.GetValue(source, null);
					if (enumerable == null)
						return null;

					foreach (var enumerableElementValue in enumerable)
					{
						if (enumerableElementValue == null)
							continue;

						contentSections = contentSections.AddRange(
							GetContent(enumerableElementValue)
						);
					}
					continue;
				}

				var nestedTypeValue = property.GetValue(source, null);
				if (nestedTypeValue != null)
				{
					contentSections = contentSections.AddRange(
						GetContent(nestedTypeValue)
					);
				}
			}
			return contentSections;
		}
	}
}
