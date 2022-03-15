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
		private readonly ITokenBreaker _tokenBreaker;
		private readonly IndexGenerator.WeightedEntryCombiner _weightedEntryCombinerOverride;
		private readonly NonNullImmutableList<IModifyMatchWeights> _propertyWeightAppliers;
		private readonly AutomatedIndexGeneratorFactory<TSource, TKey>.WeightDeterminerGenerator _tokenWeightDeterminerGeneratorOverride;
		private readonly PropertyInfo _optionalPropertyForFirstContentRetriever;
		private readonly bool _captureSourceLocations;
		private readonly ILogger _loggerOverride;
		private AutomatedIndexGeneratorFactoryBuilder(
			Func<TSource, TKey> keyRetrieverOverride,
			IEqualityComparer<TKey> keyComparerOverride,
			IStringNormaliser stringNormaliserOverride,
			ITokenBreaker tokenBreaker,
			IndexGenerator.WeightedEntryCombiner weightedEntryCombinerOverride,
			NonNullImmutableList<IModifyMatchWeights> propertyWeightAppliers,
			AutomatedIndexGeneratorFactory<TSource, TKey>.WeightDeterminerGenerator tokenWeightDeterminerGeneratorOverride,
			PropertyInfo optionalPropertyForFirstContentRetriever,
			bool captureSourceLocations,
			ILogger loggerOverride)
		{
			_keyRetrieverOverride = keyRetrieverOverride;
			_keyComparerOverride = keyComparerOverride;
			_stringNormaliserOverride = stringNormaliserOverride;
			_tokenBreaker = tokenBreaker ?? throw new ArgumentNullException(nameof(tokenBreaker));
			_weightedEntryCombinerOverride = weightedEntryCombinerOverride;
			_propertyWeightAppliers = propertyWeightAppliers ?? throw new ArgumentNullException(nameof(propertyWeightAppliers));
			_tokenWeightDeterminerGeneratorOverride = tokenWeightDeterminerGeneratorOverride;
			_optionalPropertyForFirstContentRetriever = optionalPropertyForFirstContentRetriever;
			_captureSourceLocations = captureSourceLocations;
			_loggerOverride = loggerOverride;
		}
		public AutomatedIndexGeneratorFactoryBuilder() : this(null, null, null, GetDefaultTokenBreaker(), null, NonNullImmutableList<IModifyMatchWeights>.Empty, null, null, captureSourceLocations: true, loggerOverride: null) { }

		public AutomatedIndexGeneratorFactoryBuilder<TSource, TKey> SetKeyRetriever(Func<TSource, TKey> keyRetriever)
		{
			if (keyRetriever == null)
				throw new ArgumentNullException(nameof(keyRetriever));

			return new AutomatedIndexGeneratorFactoryBuilder<TSource, TKey>(
				keyRetriever,
				_keyComparerOverride,
				_stringNormaliserOverride,
				_tokenBreaker,
				_weightedEntryCombinerOverride,
				_propertyWeightAppliers,
				_tokenWeightDeterminerGeneratorOverride,
				_optionalPropertyForFirstContentRetriever,
				_captureSourceLocations,
				_loggerOverride
			);
		}

		public AutomatedIndexGeneratorFactoryBuilder<TSource, TKey> SetKeyComparer(IEqualityComparer<TKey> keyComparer)
		{
			if (keyComparer == null)
				throw new ArgumentNullException(nameof(keyComparer));

			return new AutomatedIndexGeneratorFactoryBuilder<TSource, TKey>(
				_keyRetrieverOverride,
				keyComparer,
				_stringNormaliserOverride,
				_tokenBreaker,
				_weightedEntryCombinerOverride,
				_propertyWeightAppliers,
				_tokenWeightDeterminerGeneratorOverride,
				_optionalPropertyForFirstContentRetriever,
				_captureSourceLocations,
				_loggerOverride
			);
		}

		public AutomatedIndexGeneratorFactoryBuilder<TSource, TKey> SetStringNormaliser(IStringNormaliser stringNormaliser)
		{
			if (stringNormaliser == null)
				throw new ArgumentNullException(nameof(stringNormaliser));

			return new AutomatedIndexGeneratorFactoryBuilder<TSource, TKey>(
				_keyRetrieverOverride,
				_keyComparerOverride,
				stringNormaliser,
				_tokenBreaker,
				_weightedEntryCombinerOverride,
				_propertyWeightAppliers,
				_tokenWeightDeterminerGeneratorOverride,
				_optionalPropertyForFirstContentRetriever,
				_captureSourceLocations,
				_loggerOverride
			);
		}

		public AutomatedIndexGeneratorFactoryBuilder<TSource, TKey> SetTokenBreaker(ITokenBreaker tokenBreaker)
		{
			if (tokenBreaker == null)
				throw new ArgumentNullException(nameof(tokenBreaker));

			return new AutomatedIndexGeneratorFactoryBuilder<TSource, TKey>(
				_keyRetrieverOverride,
				_keyComparerOverride,
				_stringNormaliserOverride,
				tokenBreaker,
				_weightedEntryCombinerOverride,
				_propertyWeightAppliers,
				_tokenWeightDeterminerGeneratorOverride,
				_optionalPropertyForFirstContentRetriever,
				_captureSourceLocations,
				_loggerOverride
			);
		}

		public delegate ITokenBreaker TokenBreakerInjector(ITokenBreaker currentTokenBreaker);
		public AutomatedIndexGeneratorFactoryBuilder<TSource, TKey> InjectTokenBreaker(TokenBreakerInjector injector)
		{
			if (injector == null)
				throw new ArgumentNullException(nameof(injector));

			var newTokenBreaker = injector(_tokenBreaker);
			if (newTokenBreaker == null)
				throw new ArgumentException("Returned null reference - invalid", nameof(injector));

			return new AutomatedIndexGeneratorFactoryBuilder<TSource, TKey>(
				_keyRetrieverOverride,
				_keyComparerOverride,
				_stringNormaliserOverride,
				newTokenBreaker,
				_weightedEntryCombinerOverride,
				_propertyWeightAppliers,
				_tokenWeightDeterminerGeneratorOverride,
				_optionalPropertyForFirstContentRetriever,
				_captureSourceLocations,
				_loggerOverride
			);
		}

		public AutomatedIndexGeneratorFactoryBuilder<TSource, TKey> SetWeightedEntryCombiner(IndexGenerator.WeightedEntryCombiner weightedEntryCombiner)
		{
			if (weightedEntryCombiner == null)
				throw new ArgumentNullException(nameof(weightedEntryCombiner));

			return new AutomatedIndexGeneratorFactoryBuilder<TSource, TKey>(
				_keyRetrieverOverride,
				_keyComparerOverride,
				_stringNormaliserOverride,
				_tokenBreaker,
				weightedEntryCombiner,
				_propertyWeightAppliers,
				_tokenWeightDeterminerGeneratorOverride,
				_optionalPropertyForFirstContentRetriever,
				_captureSourceLocations,
				_loggerOverride
			);
		}

		/// <summary>
		/// If this is specified then it will take precedence over any behaviour specified through Ignore or SetWeightMultiplier - these will be ignored
		/// </summary>
		public AutomatedIndexGeneratorFactoryBuilder<TSource, TKey> SetTokenWeightDeterminer(
			AutomatedIndexGeneratorFactory<TSource, TKey>.WeightDeterminerGenerator weightDeterminerGenerator)
		{
			if (weightDeterminerGenerator == null)
				throw new ArgumentNullException(nameof(weightDeterminerGenerator));

			return new AutomatedIndexGeneratorFactoryBuilder<TSource, TKey>(
				_keyRetrieverOverride,
				_keyComparerOverride,
				_stringNormaliserOverride,
				_tokenBreaker,
				_weightedEntryCombinerOverride,
				_propertyWeightAppliers,
				weightDeterminerGenerator,
				_optionalPropertyForFirstContentRetriever,
				_captureSourceLocations,
				_loggerOverride
			);
		}

		/// <summary>
		/// A particular property can be targetted to be responsible for the first content retriever to be generated - this would mean that any SourceFieldLocations in
		/// match data with a SourceFieldIndex of zero would be guaranteed to have come from this property (if no content was extracted from this property for a given
		/// source data instance then no source locations with a SourceFieldIndex of zero would be returned). This can be useful for search term highlighting since it
		/// is common for the data for one particular field to be displayed with matched terms highlighted (the property that contains this content would be specified
		/// here). This will only work reliably if the property returns zero or one content strings for any given result - if the property delivers multiple content
		/// strings (eg. if lists all of the Tags that a Blog Post has associated with it) then each extracted content string will have a distint SourceFieldIndex
		/// value, only the first one would be assigned a SourceFieldIndex of zero.
		/// </summary>
		public AutomatedIndexGeneratorFactoryBuilder<TSource, TKey> SetPropertyForFirstContentRetriever(PropertyInfo property)
		{
			if (property == null)
				throw new ArgumentNullException(nameof(property));

			return new AutomatedIndexGeneratorFactoryBuilder<TSource, TKey>(
				_keyRetrieverOverride,
				_keyComparerOverride,
				_stringNormaliserOverride,
				_tokenBreaker,
				_weightedEntryCombinerOverride,
				_propertyWeightAppliers,
				_tokenWeightDeterminerGeneratorOverride,
				property,
				_captureSourceLocations,
				_loggerOverride
			);
		}

		/// <summary>
		/// If a value is specified through SetTokenWeightDeterminer then any calls to Ignore or SetWeightMultiplier will be ignored as that takes precedence. If a
		/// property marked to be ignored is a type that has properties beneath it, they will be ignored as well.
		/// </summary>
		public AutomatedIndexGeneratorFactoryBuilder<TSource, TKey> Ignore(PropertyInfo property)
		{
			if (property == null)
				throw new ArgumentNullException(nameof(property));

			return new AutomatedIndexGeneratorFactoryBuilder<TSource, TKey>(
				_keyRetrieverOverride,
				_keyComparerOverride,
				_stringNormaliserOverride,
				_tokenBreaker,
				_weightedEntryCombinerOverride,
				_propertyWeightAppliers.Add(new SpecificPropertyWeightApplier(property, 0)),
				_tokenWeightDeterminerGeneratorOverride,
				_optionalPropertyForFirstContentRetriever,
				_captureSourceLocations,
				_loggerOverride
			);
		}

		/// <summary>
		/// If a value is specified through SetTokenWeightDeterminer then any calls to Ignore or SetWeightMultiplier will be ignored as that takes precedence. If a
		/// property marked to be ignored is a type that has properties beneath it, they will be ignored as well. The typeName specified should be the FullName of
		/// the type (eg. "FullTextIndexer.Helpers.AutomatedIndexGeneratorFactoryBuilder").
		/// </summary>
		public AutomatedIndexGeneratorFactoryBuilder<TSource, TKey> Ignore(string typeAndPropertyName)
		{
			if (string.IsNullOrWhiteSpace(typeAndPropertyName))
				throw new ArgumentException("Null/blank typeAndPropertyName specified");

			var lastBreakPoint = typeAndPropertyName.LastIndexOf(".");
			if (lastBreakPoint == -1)
				throw new ArgumentException("must be at least two parts (type name and property name)", nameof(typeAndPropertyName));

			return new AutomatedIndexGeneratorFactoryBuilder<TSource, TKey>(
				_keyRetrieverOverride,
				_keyComparerOverride,
				_stringNormaliserOverride,
				_tokenBreaker,
				_weightedEntryCombinerOverride,
				_propertyWeightAppliers.Add(new SpecificNamedPropertyWeightApplier(typeAndPropertyName, 0)),
				_tokenWeightDeterminerGeneratorOverride,
				_optionalPropertyForFirstContentRetriever,
				_captureSourceLocations,
				_loggerOverride
			);
		}

		/// <summary>
		/// If the source location data is not important then the index generator can be configured to not capture it - this will make some functionality impossible,
		/// such as knowing precisely where in the original data matches were found (which may be used for displaying a snippet of the original content where the
		/// first match is found, for example) or using the GetConsecutiveMatches extension method (since that needs to know where each token was matched in content
		/// in order to identify when words in a phrase appear next to each other).
		/// </summary>
		public AutomatedIndexGeneratorFactoryBuilder<TSource, TKey> DisableSourceLocationCapture(bool disable = true)
		{
			return new AutomatedIndexGeneratorFactoryBuilder<TSource, TKey>(
				_keyRetrieverOverride,
				_keyComparerOverride,
				_stringNormaliserOverride,
				_tokenBreaker,
				_weightedEntryCombinerOverride,
				_propertyWeightAppliers,
				_tokenWeightDeterminerGeneratorOverride,
				_optionalPropertyForFirstContentRetriever,
				captureSourceLocations: !disable,
				loggerOverride: _loggerOverride
			);
		}

		/// <summary>
		/// If a value is specified through SetTokenWeightDeterminer then any calls to Ignore or SetWeightMultiplier will be ignored as that takes precedence. This
		/// must target a specific property, there is no cumulative effect unlike Ignore (which can affect properties of any types beneath the specified property).
		/// </summary>
		public AutomatedIndexGeneratorFactoryBuilder<TSource, TKey> SetWeightMultiplier(PropertyInfo property, float weightMultiplier)
		{
			if (property == null)
				throw new ArgumentNullException(nameof(property));
			if (weightMultiplier <= 0)
				throw new ArgumentOutOfRangeException(nameof(weightMultiplier), "must be greater than zero");

			return new AutomatedIndexGeneratorFactoryBuilder<TSource, TKey>(
				_keyRetrieverOverride,
				_keyComparerOverride,
				_stringNormaliserOverride,
				_tokenBreaker,
				_weightedEntryCombinerOverride,
				_propertyWeightAppliers.Add(new SpecificPropertyWeightApplier(property, weightMultiplier)),
				_tokenWeightDeterminerGeneratorOverride,
				_optionalPropertyForFirstContentRetriever,
				_captureSourceLocations,
				_loggerOverride
			);
		}

		/// <summary>
		/// If a value is specified through SetTokenWeightDeterminer then any calls to Ignore or SetWeightMultiplier will be ignored as that takes precedence. This
		/// must target a specific property, there is no cumulative effect unlike Ignore (which can affect properties of any types beneath the specified property).
		/// The typeName specified should be the FullName of the type (eg. "FullTextIndexer.Helpers.AutomatedIndexGeneratorFactoryBuilder").
		/// </summary>
		public AutomatedIndexGeneratorFactoryBuilder<TSource, TKey> SetWeightMultiplier(string typeAndPropertyName, float weightMultiplier)
		{
			if (string.IsNullOrWhiteSpace(typeAndPropertyName))
				throw new ArgumentException("Null/blank typeAndPropertyName specified");
			if (weightMultiplier <= 0)
				throw new ArgumentOutOfRangeException(nameof(weightMultiplier), "must be greater than zero");

			var lastBreakPoint = typeAndPropertyName.LastIndexOf(".");
			if (lastBreakPoint == -1)
				throw new ArgumentException("must be at least two parts (type name and property name)", nameof(typeAndPropertyName));

			return new AutomatedIndexGeneratorFactoryBuilder<TSource, TKey>(
				_keyRetrieverOverride,
				_keyComparerOverride,
				_stringNormaliserOverride,
				_tokenBreaker,
				_weightedEntryCombinerOverride,
				_propertyWeightAppliers.Add(new SpecificNamedPropertyWeightApplier(typeAndPropertyName, weightMultiplier)),
				_tokenWeightDeterminerGeneratorOverride,
				_optionalPropertyForFirstContentRetriever,
				_captureSourceLocations,
				_loggerOverride
			);
		}

		public AutomatedIndexGeneratorFactoryBuilder<TSource, TKey> SetLogger(ILogger logger)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));

			return new AutomatedIndexGeneratorFactoryBuilder<TSource, TKey>(
				_keyRetrieverOverride,
				_keyComparerOverride,
				_stringNormaliserOverride,
				_tokenBreaker,
				_weightedEntryCombinerOverride,
				_propertyWeightAppliers,
				_tokenWeightDeterminerGeneratorOverride,
				_optionalPropertyForFirstContentRetriever,
				_captureSourceLocations,
				logger
			);
		}

		public AutomatedIndexGeneratorFactory<TSource, TKey> Get()
		{
			var stringNormaliser = _stringNormaliserOverride ?? GetDefaultStringNormaliser();
			return new AutomatedIndexGeneratorFactory<TSource, TKey>(
				_keyRetrieverOverride ?? GetDefaultKeyRetriever(),
				_keyComparerOverride ?? new DefaultEqualityComparer<TKey>(),
				stringNormaliser,
				_tokenBreaker ?? GetDefaultTokenBreaker(),
				_weightedEntryCombinerOverride ?? (weightedValues => weightedValues.Sum()),
				_tokenWeightDeterminerGeneratorOverride ?? GetDefaultTokenWeightDeterminerGenerator(stringNormaliser),
				_optionalPropertyForFirstContentRetriever,
				_captureSourceLocations,
				_loggerOverride ?? new NullLogger()
			);
		}

		private static Func<TSource, TKey> GetDefaultKeyRetriever()
		{
			var keyRetriever = TryToGetAsKeyRetriever("Key") ?? TryToGetAsKeyRetriever("Id");
			if (keyRetriever == null)
				throw new ArgumentException("Unable to automate key retrieval through either \"Key\" or \"Id\" properties");

			return keyRetriever;
		}

		private static Func<TSource, TKey> TryToGetAsKeyRetriever(string propertyName)
		{
			if (string.IsNullOrWhiteSpace(propertyName))
				throw new ArgumentException("Null/blank propertyName specified");

			var property = typeof(TSource).GetProperty(propertyName);
			if ((property == null) || !typeof(TKey).IsAssignableFrom(property.PropertyType))
				return null;

			return entry => (TKey)property.GetValue(entry, null);
		}

		private static IStringNormaliser GetDefaultStringNormaliser()
		{
			return new EnglishPluralityStringNormaliser(
				DefaultStringNormaliser.Instance,
				EnglishPluralityStringNormaliser.PreNormaliserWorkOptions.PreNormaliserLowerCases | EnglishPluralityStringNormaliser.PreNormaliserWorkOptions.PreNormaliserTrims
			);
		}

		private static ITokenBreaker GetDefaultTokenBreaker()
		{
			return new WhiteSpaceExtendingTokenBreaker(
				new ImmutableList<char>(
					'<', '>', '[', ']', '(', ')', '{', '}',
					'.', ',', ':', ';', '"', '?', '!',
					'/', '\\',
					'@', '+', '|', '='
				),
				new WhiteSpaceTokenBreaker()
			);
		}

		private AutomatedIndexGeneratorFactory<TSource, TKey>.WeightDeterminerGenerator GetDefaultTokenWeightDeterminerGenerator(IStringNormaliser stringNormaliser)
		{
			if (stringNormaliser == null)
				throw new ArgumentNullException(nameof(stringNormaliser));

			// Constructing a HashSet of the normalised versions of the stop words means that looking up whether normalised tokens are stop
			// words can be a lot faster (as neither the stop words nor the token need to be fed through the normaliser again)
			var hashSetOfNormalisedStopWords = new HashSet<string>(
				Constants.GetStopWords("en").Select(word => stringNormaliser.GetNormalisedString(word))
			);
			return property =>
			{
				// Reverse the propertyWeightAppliers so that later values added to the set take precedence (eg. if, for some reason, a x5 weight is
				// given to a property and then later it's set to be ignored, then we want to ignore it - which this will achieve)
				var propertyWeightApplier = _propertyWeightAppliers.Reverse().FirstOrDefault(p => p.AppliesTo(property));
				if ((propertyWeightApplier != null) && (propertyWeightApplier.WeightMultiplier == 0))
				{
					// A weight multiplier of zero means ignore this property, as does returning null from a WeightDeterminerGenerator call
					return null;
				}

				var weightMultiplier = (propertyWeightApplier != null) ? propertyWeightApplier.WeightMultiplier : 1;
				return normalisedToken => weightMultiplier * (hashSetOfNormalisedStopWords.Contains(normalisedToken) ? 0.01f : 1f);
			};
		}

		private interface IModifyMatchWeights
		{
			/// <summary>
			/// This should raise an exception for a null property reference
			/// </summary>
			bool AppliesTo(PropertyInfo property);

			/// <summary>
			/// This must always be zero or greater
			/// </summary>
			float WeightMultiplier { get; }
		}

		private abstract class PropertyWeightApplier : IModifyMatchWeights
		{
			public PropertyWeightApplier(float weightMultiplier)
			{
				if (weightMultiplier < 0)
					throw new ArgumentOutOfRangeException(nameof(weightMultiplier), "must be zero or greater");

				WeightMultiplier = weightMultiplier;
			}

			/// <summary>
			/// This should raise an exception for a null property reference
			/// </summary>
			public abstract bool AppliesTo(PropertyInfo property);

			/// <summary>
			/// This will always be zero or greater
			/// </summary>
			public float WeightMultiplier { get; private set; }
		}

		private class SpecificPropertyWeightApplier : PropertyWeightApplier
		{
			private readonly PropertyInfo _property;
			public SpecificPropertyWeightApplier(PropertyInfo property, float weightMultiplier) : base(weightMultiplier)
			{
                _property = property ?? throw new ArgumentNullException(nameof(property));
			}

			/// <summary>
			/// This will raise an exception for a null property reference
			/// </summary>
			public override bool AppliesTo(PropertyInfo property)
			{
				if (property == null)
					throw new ArgumentNullException(nameof(property));

				return property == _property;
			}
		}

		private class SpecificNamedPropertyWeightApplier : PropertyWeightApplier
		{
			private readonly string _typeName, _propertyName;
			public SpecificNamedPropertyWeightApplier(string typeAndPropertyName, float weightMultiplier) : base(weightMultiplier)
			{
				if (string.IsNullOrWhiteSpace(typeAndPropertyName))
					throw new ArgumentException("Null/blank typeAndPropertyName specified");

				var lastBreakPoint = typeAndPropertyName.LastIndexOf(".");
				if (lastBreakPoint == -1)
					throw new ArgumentException("must be at least two parts (type name and property name)", nameof(typeAndPropertyName));

				_typeName = typeAndPropertyName.Substring(0, lastBreakPoint);
				_propertyName = typeAndPropertyName.Substring(lastBreakPoint + 1);
			}

			/// <summary>
			/// This will raise an exception for a null property reference
			/// </summary>
			public override bool AppliesTo(PropertyInfo property)
			{
				if (property == null)
					throw new ArgumentNullException(nameof(property));

				return (property.DeclaringType.FullName == _typeName) && (property.Name == _propertyName);
			}
		}
	}
}
