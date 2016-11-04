using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
#if NET452
using System.Runtime.Serialization;
#endif
using System.Security;

namespace FullTextIndexer.Core.Indexes.TernarySearchTree
{
	/// <summary>
	/// This will match common strings where one is the plural and the other the singular version of the same word. It not intended to be perfect and may
	/// match a few false positives, but it should catch most of the most common cases.
	/// </summary>
#if NET452
	[Serializable]
#endif
	public class EnglishPluralityStringNormaliser : StringNormaliser
#if NET452
		, ISerializable
#endif
	{
		private readonly List<PluralEntry> _plurals;
		private readonly Func<string, string> _normaliser;
		private readonly IStringNormaliser _optionalPreNormaliser;
		private readonly PreNormaliserWorkOptions _preNormaliserWork;
		public EnglishPluralityStringNormaliser(IEnumerable<PluralEntry> plurals, IStringNormaliser optionalPreNormaliser, PreNormaliserWorkOptions preNormaliserWork)
		{
			if (plurals == null)
				throw new ArgumentNullException("pluralEntries");
			var allPreNormaliserOptions = (PreNormaliserWorkOptions)0;
			foreach (PreNormaliserWorkOptions option in Enum.GetValues(typeof(PreNormaliserWorkOptions)))
				allPreNormaliserOptions = allPreNormaliserOptions | option;
			if ((preNormaliserWork & allPreNormaliserOptions) != preNormaliserWork)
				throw new ArgumentOutOfRangeException("preNormaliserWork");

			var pluralsTidied = new List<PluralEntry>();
			foreach (var plural in plurals)
			{
				if (plural == null)
					throw new ArgumentException("Null reference encountered in plurals set");
				pluralsTidied.Add(plural);
			}

			// Although we don't need the plurals once the normaliser has been generated in normal operation, if the instance is to be serialised then we need to record
			// them so that the normalier can be re-generated at deserialisation (as the normaliser that is generated can not be serialised - see GetObjectData)
			_plurals = pluralsTidied;
			_normaliser = GenerateNormaliser();
			_optionalPreNormaliser = optionalPreNormaliser;
			_preNormaliserWork = preNormaliserWork;
		}

		public EnglishPluralityStringNormaliser(IStringNormaliser optionalPreNormaliser, PreNormaliserWorkOptions preNormaliserWork)
			: this(DefaultPlurals, optionalPreNormaliser, preNormaliserWork) { }

		public EnglishPluralityStringNormaliser() : this(null, PreNormaliserWorkOptions.PreNormaliserDoesNothing) { }

#if NET452
		protected EnglishPluralityStringNormaliser(SerializationInfo info, StreamingContext context)
			: this(
				(IEnumerable<PluralEntry>)info.GetValue("_plurals", typeof(IEnumerable<PluralEntry>)),
				(IStringNormaliser)info.GetValue("_optionalPreNormaliser", typeof(IStringNormaliser)),
				(PreNormaliserWorkOptions)info.GetValue("_preNormaliserWork", typeof(PreNormaliserWorkOptions))
			) { }

		[SecurityCritical]
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			// Unfortunately we can't serialise the generated normaliser (we'll get a "Cannot serialize delegates over unmanaged function pointers, dynamic
			// methods or methods outside the delegate creator's assembly" error) so if we have to serialise this instance we'll store all of the dat and
			// then re-generate the normaliser on de-serialisation. Not ideal from a performance point of view but at least it will work.
			info.AddValue("_plurals", _plurals);
			info.AddValue("_optionalPreNormaliser", _optionalPreNormaliser);
			info.AddValue("_preNormaliserWork", _preNormaliserWork);
		}
#endif

#if NET452
	[Serializable]
#endif
		[Flags]
		public enum PreNormaliserWorkOptions
		{
			PreNormaliserDoesNothing = 0,
			PreNormaliserLowerCases = 1,
			PreNormaliserTrims = 2
		}

		public override string GetNormalisedString(string value)
		{
			if (value == null)
				throw new ArgumentNullException("value");

			// If an additional normaliser was specified in the constructor then process the string with that first (eg. a normaliser that removes punctuation
			// from values may be beneficial depending upon the the content that may be passed in)
			if (_optionalPreNormaliser != null)
				value = _optionalPreNormaliser.GetNormalisedString(value);

			if ((_preNormaliserWork & PreNormaliserWorkOptions.PreNormaliserTrims) != PreNormaliserWorkOptions.PreNormaliserTrims)
				value = value.Trim();
			if (value == "")
				return "";

			// We have to lower case the trimmed value since the suffixes are all stored as lower case values
			if ((_preNormaliserWork & PreNormaliserWorkOptions.PreNormaliserLowerCases) != PreNormaliserWorkOptions.PreNormaliserLowerCases)
				value = value.ToLower();
			return _normaliser(value);
		}

		private Func<string, string> GenerateNormaliser()
		{
			// Build up if statements for each suffix - if a match is found, return the input value with the matched suffix replaced
			// with a combination of all the other suffixes in PluralEntry
			var result = Expression.Parameter(typeof(string), "result");
			var endLabel = Expression.Label(typeof(string));
			var valueTrimmed = Expression.Parameter(typeof(string), "valueTrimmed");
			var expressions = new List<Expression>();
			foreach (var plural in _plurals)
			{
				foreach (var suffix in plural.Values)
				{
					// If the pluralisation rule applies to the value then return either the stem (the value minus the suffix, if
					// the rule has a SuffixOnly match type, or the first of the plural's suffix values, if the rule's match type
					// is WholeWord - otherwise a blank string would always be returned for WholeWord matches). The "~" character
					// is appended to the returned content so that if a string is fed through the normalisation process multiple
					// times then the return values will be stable (eg. "cats" becomes "cat~" which stays at "cat~").
					expressions.Add(
						Expression.IfThen(
							GeneratePredicate(suffix, valueTrimmed, plural.MatchType),
							Expression.Block(
								Expression.Assign(
									result,
									plural.MatchType == MatchTypeOptions.SuffixOnly
										? GenerateAppendStringExpression(GenerateRemoveLastCharactersExpression(valueTrimmed, suffix.Length), "~")
										: Expression.Constant(plural.Values[0] + "~")
								),
								Expression.Return(endLabel, result)
							)
						)
				   );
				}
			}

			// Insert an expression to check whether the value already ends with a "~" and, if so, return it (we can safely do
			// this without worrying about whether the input value is a plural that ends with a "~" that should be processed
			// because this class will not consider "cats~" to be the plural of "cat~" or "cat", it checks the end of the word
			// and so will not make any matches for words that end with "~"). Value that end with "~" are either values that
			// have been manipulated by this process already or are values that this process should not alter.
			expressions.Insert(
				0,
				Expression.IfThen(
					GeneratePredicate("~", valueTrimmed, MatchTypeOptions.SuffixOnly),
					Expression.Block(
						Expression.Assign(
							result,
							valueTrimmed
						),
						Expression.Return(endLabel, result)
					)
				)
			);

			// If none of the suffixes apply (and the value does not already have a trailing "~") then append the "~"
			expressions.Add(
				Expression.Assign(
					result,
					GenerateAppendStringExpression(valueTrimmed, "~")
				)
			);

			// Add the return-point label, configured to return the string value in "result"
			expressions.Add(Expression.Label(endLabel, result));

			return Expression.Lambda<Func<string, string>>(
				Expression.Block(
					new[] { result },
					expressions
				),
				valueTrimmed
			).Compile();
		}

		/// <summary>
		/// Generate an expression that determines whether a string parameter matches a specified suffix / matchType combination
		/// </summary>
		private static Expression GeneratePredicate(string suffix, ParameterExpression valueTrimmed, MatchTypeOptions matchType)
		{
			if (string.IsNullOrWhiteSpace(suffix))
				throw new ArgumentException("Null/blank suffix specified");
			if (valueTrimmed == null)
				throw new ArgumentNullException("valueTrimmed");
			if (!Enum.IsDefined(typeof(MatchTypeOptions), matchType))
				throw new ArgumentOutOfRangeException("matchType");

			suffix = suffix.Trim();

			var conditionElements = new List<Expression>();
			var lengthProperty = typeof(string).GetProperty("Length");
			var indexedProperty = typeof(string).GetProperties().First(p => (p.GetIndexParameters() ?? new ParameterInfo[0]).Any());
			if (matchType == MatchTypeOptions.SuffixOnly)
			{
				conditionElements.Add(
					Expression.GreaterThan(
						Expression.Property(valueTrimmed, lengthProperty),
						Expression.Constant(suffix.Length, typeof(int))
					)
				);
			}
			else
			{
				conditionElements.Add(
					Expression.Equal(
						Expression.Property(valueTrimmed, lengthProperty),
						Expression.Constant(suffix.Length, typeof(int))
					)
				);
			}
			for (var index = 0; index < suffix.Length; index++)
			{
				conditionElements.Add(
					Expression.Equal(
						Expression.Constant(suffix[index], typeof(char)),
						Expression.Property(
							valueTrimmed,
							indexedProperty,
							Expression.Subtract(
								Expression.Property(valueTrimmed, lengthProperty),
								Expression.Constant(suffix.Length - index, typeof(int))
							)
						)
					)
				);
			}
			return CombineExpressionsWithAndAlso(conditionElements);
		}

		private static Expression CombineExpressionsWithAndAlso(IEnumerable<Expression> expressions)
		{
			if (expressions == null)
				throw new ArgumentNullException("expressions");

			var expressionsTidied = new List<Expression>();
			foreach (var expression in expressions)
			{
				if (expression == null)
					throw new ArgumentException("Null reference encountered in expressions set");
				expressionsTidied.Add(expression);
			}
			if (!expressionsTidied.Any())
				throw new Exception("No entries in expressions set");
			else if (expressionsTidied.Count == 1)
				return expressionsTidied[0];

			var reducedExpressions = new List<Expression>();
			for (var index = 0; index < expressionsTidied.Count; index += 2)
			{
				var expression = expressionsTidied[index];
				if (index < (expressionsTidied.Count - 1))
				{
					var expressionNext = expressionsTidied[index + 1];
					reducedExpressions.Add(Expression.AndAlso(expression, expressionNext));
				}
				else
					reducedExpressions.Add(expression);
			}

			return (reducedExpressions.Count == 1) ? reducedExpressions[0] : CombineExpressionsWithAndAlso(reducedExpressions);
		}

		/// <summary>
		/// The value Expression must represent a non-null string that is as at least as long as the specified length or an exception will
		/// be thrown upon exection
		/// </summary>
		private static Expression GenerateRemoveLastCharactersExpression(Expression value, int length)
		{
			if (value == null)
				throw new ArgumentNullException("value");
			if (length < 0)
				throw new ArgumentOutOfRangeException("length");

			return Expression.Call(
				value,
				typeof(string).GetMethod("Substring", new[] { typeof(int), typeof(int) }),
				Expression.Constant(0),
				Expression.Subtract(
					Expression.Property(value, typeof(string).GetProperty("Length")),
					Expression.Constant(length, typeof(int))
				)
			);
		}

		private static Expression GenerateAppendStringExpression(Expression value, string toAppend)
		{
			if (value == null)
				throw new ArgumentNullException("value");
			if (toAppend == null)
				throw new ArgumentNullException("toAppend");

			return Expression.Call(
				typeof(String).GetMethod("Concat", new[] { typeof(string), typeof(string) }),
				value,
				Expression.Constant(toAppend)
			);
		}

		private static string CreateSuffixExtension(IEnumerable<string> suffixes)
		{
			if (suffixes == null)
				throw new ArgumentNullException("suffixes");

			var suffixesTidied = suffixes.ToList();
			if (!suffixesTidied.Any())
				throw new ArgumentException("No entries in suffixes set");
			if (suffixesTidied.Any(s => string.IsNullOrWhiteSpace(s)))
				throw new ArgumentException("Null/blank entry encountered in suffixes set");

			return "|" + string.Join("|", suffixesTidied.Select(s => s.Trim()));
		}

		/// <summary>
		/// Given a set of values, ensure that none are null and return them de-duplicated after having been pushed through a string manipulation.
		/// This will throw an exception for null arguments or if any null value is encountered in the values set.
		/// </summary>
		private static List<string> TidyStringList(IEnumerable<string> values, Func<string, string> transformer)
		{
			if (values == null)
				throw new ArgumentNullException("values");
			if (transformer == null)
				throw new ArgumentNullException("transformer");

			var valuesTidied = new List<string>();
			foreach (var value in values)
			{
				if (value == null)
					throw new ArgumentException("Null entry encountered in values");

				var valueToStore = transformer(value);
				if (!valuesTidied.Contains(valueToStore))
					valuesTidied.Add(valueToStore);
			}
			return valuesTidied.Distinct().ToList();
		}

		public readonly static PluralEntry[] DefaultPlurals = new[]
		{
			// eg. formula / formulae / formulas
			new PluralEntry(new[] { "ula", "ulae", "ulas" }, MatchTypeOptions.SuffixOnly),

			// eg. category / categories
			new PluralEntry(new[] { "y", "ies" }, MatchTypeOptions.SuffixOnly),

			// eg. cactus / cactii
			new PluralEntry(new[] { "us", "ii" }, MatchTypeOptions.SuffixOnly),

			// eg. child / children
			new PluralEntry(new[] { "ld", "ldren" }, MatchTypeOptions.SuffixOnly),

			// eg. medium / media
			new PluralEntry(new[] { "ium", "ia" }, MatchTypeOptions.SuffixOnly),

			// eg. one / ones,  tone / tones
			new PluralEntry(new[] { "ne", "nes" }, MatchTypeOptions.SuffixOnly),

			// eg. tome / tomes (won't include "me" as SuffixOnly requires that the word length be greater than the suffix length)
			new PluralEntry(new[] { "me", "mes" }, MatchTypeOptions.SuffixOnly),

			// eg. technique / techniques
			new PluralEntry(new[] { "ue", "ues" }, MatchTypeOptions.SuffixOnly),

			// Common special cases that have to come before the "ses", es", "s" form
			new PluralEntry(new[] { "index", "indexes", "indices" }, MatchTypeOptions.WholeWord),
			new PluralEntry(new[] { "matrix", "matrices" }, MatchTypeOptions.WholeWord),
			new PluralEntry(new[] { "vertex", "vertices" }, MatchTypeOptions.WholeWord),

			// eg. Abacuses, matching "s" here means we must use "ses", "es" AND "s" as fallbacks below
			new PluralEntry(new[] { "ses", "es", "s" }, MatchTypeOptions.SuffixOnly),

			// Other common special cases
			new PluralEntry(new[] { "datum", "data" }, MatchTypeOptions.WholeWord),
			new PluralEntry(new[] { "man", "men" }, MatchTypeOptions.WholeWord),
			new PluralEntry(new[] { "woman", "women" }, MatchTypeOptions.WholeWord)
		};

#if NET452
	[Serializable]
#endif
		public class PluralEntry
		{
			public PluralEntry(IEnumerable<string> values, MatchTypeOptions matchType)
			{
				if (values == null)
					throw new ArgumentNullException("values");
				if (!Enum.IsDefined(typeof(MatchTypeOptions), matchType))
					throw new ArgumentOutOfRangeException("matchType");

				var valuesTidied = TidyStringList(values, v => v.Trim().ToLower());
				if (!valuesTidied.Any())
					throw new ArgumentException("No entries in values set");

				Values = valuesTidied.AsReadOnly();
				MatchType = matchType;
			}

			/// <summary>
			/// This will never be null or an empty set, nor will it contain any null, empty or duplicate values (all values are lower-cased and trimmed)
			/// </summary>
			public ReadOnlyCollection<string> Values { get; private set; }

			public MatchTypeOptions MatchType { get; private set; }
		}

#if NET452
	[Serializable]
#endif
		public enum MatchTypeOptions
		{
			SuffixOnly,
			WholeWord
		}
	}
}
