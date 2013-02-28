using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FullTextIndexer.Common.Lists;
using FullTextIndexer.Common.Logging;
using FullTextIndexer.Core;
using FullTextIndexer.Core.Indexes.TernarySearchTree;
using FullTextIndexer.Core.IndexGenerators;
using FullTextIndexer.Core.TokenBreaking;

namespace FullTextIndexer.Helpers
{
	/// <summary>
	/// This is a helper class for instantiating the AutomatedIndexGeneratorFactory: default values for all of the constructor arguments will be used unless alternatives
	/// are specified provided (through the Set.. methods such as SetKeyRetriever)
	/// </summary>
	public class AutomatedIndexGeneratorFactoryBuilder<TSource, TKey> where TSource : class
	{
		private readonly Func<TSource, TKey> _keyRetrieverOverride;
		private readonly IEqualityComparer<TKey> _keyComparerOverride;
		private readonly IStringNormaliser _stringNormaliserOverride;
		private readonly ITokenBreaker _tokenBreakerOverride;
		private readonly IndexGenerator.WeightedEntryCombiner _weightedEntryCombinerOverride;
		private readonly ContentRetriever<TSource, TKey>.BrokenTokenWeightDeterminer _brokenTokenWeightDeterminerOverride;
		private readonly Predicate<PropertyInfo> _propertyFilterOverride;
		private readonly ILogger _loggerOverride;
		private AutomatedIndexGeneratorFactoryBuilder(
			Func<TSource, TKey> keyRetrieverOverride,
			IEqualityComparer<TKey> keyComparerOverride,
			IStringNormaliser stringNormaliserOverride,
			ITokenBreaker tokenBreakerOverride,
			IndexGenerator.WeightedEntryCombiner weightedEntryCombinerOverride,
			ContentRetriever<TSource, TKey>.BrokenTokenWeightDeterminer brokenTokenWeightDeterminerOverride,
			Predicate<PropertyInfo> propertyFilterOverride,
			ILogger loggerOverride)
		{
			_keyRetrieverOverride = keyRetrieverOverride;
			_keyComparerOverride = keyComparerOverride;
			_stringNormaliserOverride = stringNormaliserOverride;
			_tokenBreakerOverride = tokenBreakerOverride;
			_weightedEntryCombinerOverride = weightedEntryCombinerOverride;
			_brokenTokenWeightDeterminerOverride = brokenTokenWeightDeterminerOverride;
			_propertyFilterOverride = propertyFilterOverride;
			_loggerOverride = loggerOverride;
		}
		public AutomatedIndexGeneratorFactoryBuilder() : this(null, null, null, null, null, null, null, null) { }

		public AutomatedIndexGeneratorFactoryBuilder<TSource, TKey> SetKeyRetriever(Func<TSource, TKey> keyRetriever)
		{
			if (keyRetriever == null)
				throw new ArgumentNullException("keyRetriever");

			return new AutomatedIndexGeneratorFactoryBuilder<TSource, TKey>(
				keyRetriever,
				_keyComparerOverride,
				_stringNormaliserOverride,
				_tokenBreakerOverride,
				_weightedEntryCombinerOverride,
				_brokenTokenWeightDeterminerOverride,
				_propertyFilterOverride,
				_loggerOverride
			);
		}

		public AutomatedIndexGeneratorFactoryBuilder<TSource, TKey> SetKeyComparer(IEqualityComparer<TKey> keyComparer)
		{
			if (keyComparer == null)
				throw new ArgumentNullException("keyComparer");

			return new AutomatedIndexGeneratorFactoryBuilder<TSource, TKey>(
				_keyRetrieverOverride,
				keyComparer,
				_stringNormaliserOverride,
				_tokenBreakerOverride,
				_weightedEntryCombinerOverride,
				_brokenTokenWeightDeterminerOverride,
				_propertyFilterOverride,
				_loggerOverride
			);
		}

		public AutomatedIndexGeneratorFactoryBuilder<TSource, TKey> SetStringNormaliser(IStringNormaliser stringNormaliser)
		{
			if (stringNormaliser == null)
				throw new ArgumentNullException("stringNormaliser");

			return new AutomatedIndexGeneratorFactoryBuilder<TSource, TKey>(
				_keyRetrieverOverride,
				_keyComparerOverride,
				stringNormaliser,
				_tokenBreakerOverride,
				_weightedEntryCombinerOverride,
				_brokenTokenWeightDeterminerOverride,
				_propertyFilterOverride,
				_loggerOverride
			);
		}

		public AutomatedIndexGeneratorFactoryBuilder<TSource, TKey> SetTokenBreaker(ITokenBreaker tokenBreaker)
		{
			if (tokenBreaker == null)
				throw new ArgumentNullException("tokenBreaker");

			return new AutomatedIndexGeneratorFactoryBuilder<TSource, TKey>(
				_keyRetrieverOverride,
				_keyComparerOverride,
				_stringNormaliserOverride,
				tokenBreaker,
				_weightedEntryCombinerOverride,
				_brokenTokenWeightDeterminerOverride,
				_propertyFilterOverride,
				_loggerOverride
			);
		}

		public AutomatedIndexGeneratorFactoryBuilder<TSource, TKey> SetWeightedEntryCombiner(IndexGenerator.WeightedEntryCombiner weightedEntryCombiner)
		{
			if (weightedEntryCombiner == null)
				throw new ArgumentNullException("weightedEntryCombiner");

			return new AutomatedIndexGeneratorFactoryBuilder<TSource, TKey>(
				_keyRetrieverOverride,
				_keyComparerOverride,
				_stringNormaliserOverride,
				_tokenBreakerOverride,
				weightedEntryCombiner,
				_brokenTokenWeightDeterminerOverride,
				_propertyFilterOverride,
				_loggerOverride
			);
		}

		public AutomatedIndexGeneratorFactoryBuilder<TSource, TKey> SetBrokenTokenWeightDeterminer(ContentRetriever<TSource, TKey>.BrokenTokenWeightDeterminer brokenTokenWeightDeterminer)
		{
			if (brokenTokenWeightDeterminer == null)
				throw new ArgumentNullException("brokenTokenWeightDeterminer");

			return new AutomatedIndexGeneratorFactoryBuilder<TSource, TKey>(
				_keyRetrieverOverride,
				_keyComparerOverride,
				_stringNormaliserOverride,
				_tokenBreakerOverride,
				_weightedEntryCombinerOverride,
				brokenTokenWeightDeterminer,
				_propertyFilterOverride,
				_loggerOverride
			);
		}

		public AutomatedIndexGeneratorFactoryBuilder<TSource, TKey> SetPropertyFilter(Predicate<PropertyInfo> propertyFilter)
		{
			if (propertyFilter == null)
				throw new ArgumentNullException("propertyFilter");

			return new AutomatedIndexGeneratorFactoryBuilder<TSource, TKey>(
				_keyRetrieverOverride,
				_keyComparerOverride,
				_stringNormaliserOverride,
				_tokenBreakerOverride,
				_weightedEntryCombinerOverride,
				_brokenTokenWeightDeterminerOverride,
				propertyFilter,
				_loggerOverride
			);
		}

		public AutomatedIndexGeneratorFactoryBuilder<TSource, TKey> SetLogger(ILogger logger)
		{
			if (logger == null)
				throw new ArgumentNullException("logger");

			return new AutomatedIndexGeneratorFactoryBuilder<TSource, TKey>(
				_keyRetrieverOverride,
				_keyComparerOverride,
				_stringNormaliserOverride,
				_tokenBreakerOverride,
				_weightedEntryCombinerOverride,
				_brokenTokenWeightDeterminerOverride,
				_propertyFilterOverride,
				logger
			);
		}

		public AutomatedIndexGeneratorFactory<TSource, TKey> Get()
		{
			var stringNormaliser = _stringNormaliserOverride ?? new DefaultStringNormaliser();
			return new AutomatedIndexGeneratorFactory<TSource, TKey>(
				_keyRetrieverOverride ?? GetDefaultKeyRetriever(),
				_propertyFilterOverride ?? (property => true),
				_keyComparerOverride ?? new DefaultEqualityComparer<TKey>(),
				stringNormaliser,
				_tokenBreakerOverride ?? GetDefaultTokenBreaker(),
				_weightedEntryCombinerOverride ?? (weightedValues => weightedValues.Sum()),
				_brokenTokenWeightDeterminerOverride ?? GetDefaultTokenWeightDeterminer(stringNormaliser),
				_loggerOverride ?? new NullLogger()
			);
		}

		private Func<TSource, TKey> GetDefaultKeyRetriever()
		{
			var keyRetriever = TryToGetAsKeyRetriever("Key") ?? TryToGetAsKeyRetriever("Id");
			if (keyRetriever == null)
				throw new ArgumentException("Unable to automate key retrieval through either \"Key\" or \"Id\" properties");

			return keyRetriever;
		}

		private Func<TSource, TKey> TryToGetAsKeyRetriever(string propertyName)
		{
			if (string.IsNullOrWhiteSpace(propertyName))
				throw new ArgumentException("Null/blank propertyName specified");

			var property = typeof(TSource).GetProperty(propertyName);
			if ((property == null) || !typeof(TKey).IsAssignableFrom(property.PropertyType))
				return null;

			return entry => (TKey)property.GetValue(entry, null);
		}

		private ITokenBreaker GetDefaultTokenBreaker()
		{
			return new WhiteSpaceExtendingTokenBreaker(
				new ImmutableList<char>(new[] { '<', '>', '[', ']', '(', ')', '{', '}', '.', ',' }),
				new WhiteSpaceTokenBreaker()
			);
		}

		private ContentRetriever<TSource, TKey>.BrokenTokenWeightDeterminer GetDefaultTokenWeightDeterminer(IStringNormaliser stringNormaliser)
		{
			if (stringNormaliser == null)
				throw new ArgumentNullException("stringNormaliser");

			return token => Constants.GetStopWords("en").Contains(token, stringNormaliser) ? 0.01f : 1f;
		}
	}
}
