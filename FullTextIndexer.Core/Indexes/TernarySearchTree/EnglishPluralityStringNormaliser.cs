using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security;
using FullTextIndexer.Common.Lists;

namespace FullTextIndexer.Core.Indexes.TernarySearchTree
{
    /// <summary>
    /// This will match common strings where one is the plural and the other the singular version of the same word. It not intended to be perfect and may
    /// match a few false positives, but it should catch most of the most common cases.
    /// </summary>
    [Serializable]
	public sealed class EnglishPluralityStringNormaliser : StemmingStringNormaliser
	{
		private readonly NonNullImmutableList<PluralEntry> _plurals;
		public EnglishPluralityStringNormaliser(NonNullImmutableList<PluralEntry> plurals, IStringNormaliser optionalPreNormaliser, PreNormaliserWorkOptions preNormaliserWork)
			: base(plurals?.ToNonNullImmutableList<IStemOpportunity>(), optionalPreNormaliser, preNormaliserWork)
		{

			// Although we don't need the plurals once the normaliser has been generated in normal operation, if the instance is to be serialised then we need to record
			// them so that the normalier can be re-generated at deserialisation (as the normaliser that is generated can not be serialised - see GetObjectData)
			_plurals = plurals ?? throw new ArgumentNullException("pluralEntries");
		}

		public EnglishPluralityStringNormaliser(IStringNormaliser optionalPreNormaliser, PreNormaliserWorkOptions preNormaliserWork)
			: this(DefaultPlurals, optionalPreNormaliser, preNormaliserWork) { }

		public EnglishPluralityStringNormaliser() : this(null, PreNormaliserWorkOptions.PreNormaliserDoesNothing) { }

		private EnglishPluralityStringNormaliser(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			_plurals = (NonNullImmutableList<PluralEntry>)info.GetValue("_plurals", typeof(NonNullImmutableList<PluralEntry>));
		}

		[SecurityCritical]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("_plurals", _plurals);
			base.GetObjectData(info, context);
		}

		public readonly static NonNullImmutableList<PluralEntry> DefaultPlurals = NonNullImmutableList.Create(
			// eg. formula / formulae / formulas
			new PluralEntry(NonNullImmutableList.Create("ula", "ulae", "ulas"), MatchTypeOptions.SuffixOnly),

			// eg. category / categories
			new PluralEntry(NonNullImmutableList.Create("y", "ies"), MatchTypeOptions.SuffixOnly),

			// eg. cactus / cactii
			new PluralEntry(NonNullImmutableList.Create("us", "ii"), MatchTypeOptions.SuffixOnly),

			// eg. child / children
			new PluralEntry(NonNullImmutableList.Create("ld", "ldren"), MatchTypeOptions.SuffixOnly),

			// eg. medium / media
			new PluralEntry(NonNullImmutableList.Create("ium", "ia"), MatchTypeOptions.SuffixOnly),

			// eg. one / ones,  tone / tones
			new PluralEntry(NonNullImmutableList.Create("ne", "nes"), MatchTypeOptions.SuffixOnly),

			// eg. tome / tomes (won't include "me" as SuffixOnly requires that the word length be greater than the suffix length)
			new PluralEntry(NonNullImmutableList.Create("me", "mes"), MatchTypeOptions.SuffixOnly),

			// eg. technique / techniques
			new PluralEntry(NonNullImmutableList.Create("ue", "ues"), MatchTypeOptions.SuffixOnly),

			// Common special cases that have to come before the "ses", es", "s" form
			new PluralEntry(NonNullImmutableList.Create("index", "indexes", "indices"), MatchTypeOptions.WholeWord),
			new PluralEntry(NonNullImmutableList.Create("matrix", "matrices"), MatchTypeOptions.WholeWord),
			new PluralEntry(NonNullImmutableList.Create("vertex", "vertices"), MatchTypeOptions.WholeWord),

			// eg. Abacuses, matching "s" here means we must use "ses", "es" AND "s" as fallbacks below
			new PluralEntry(NonNullImmutableList.Create("ses", "es", "s"), MatchTypeOptions.SuffixOnly),

			// Other common special cases
			new PluralEntry(NonNullImmutableList.Create("datum", "data"), MatchTypeOptions.WholeWord),
			new PluralEntry(NonNullImmutableList.Create("man", "men"), MatchTypeOptions.WholeWord),
			new PluralEntry(NonNullImmutableList.Create("woman", "women"), MatchTypeOptions.WholeWord)
		);

		[Serializable]
		public sealed class PluralEntry : IStemOpportunity
		{
			public PluralEntry(NonNullImmutableList<string> values, MatchTypeOptions matchType)
			{
				if (values == null)
					throw new ArgumentNullException(nameof(values));
				if (!Enum.IsDefined(typeof(MatchTypeOptions), matchType))
					throw new ArgumentOutOfRangeException(nameof(matchType));

				var valuesTidied = TidyStringList(values, v => v.Trim().ToLower());
				if (!valuesTidied.Any())
					throw new ArgumentException("No entries in values set");

				Values = valuesTidied;
				MatchType = matchType;
			}

			/// <summary>
			/// This will never be null or an empty set, nor will it contain any null, empty or duplicate values (all values are lower-cased and trimmed)
			/// </summary>
			public NonNullImmutableList<string> Values { get; private set; }

			public MatchTypeOptions MatchType { get; private set; }

			/// <summary>
			/// Generate a list of expression that determines whether a string parameter matches a specified suffix / matchType combination - if any one
			/// of them indicate a match then it's a match (they don't ALL have to indicate a match) and, along with each BinaryExpression, there will be
			/// an expression to set the value to in order to be represented in its 'normalised' form
			/// </summary>
			public IEnumerable<(BinaryExpression IsMatch, Expression ReduceToIfMatch)> GeneratePredicates(ParameterExpression valueTrimmed)
			{
				if (valueTrimmed == null)
					throw new ArgumentNullException(nameof(valueTrimmed));

				var lengthProperty = typeof(string).GetProperty("Length");

				var indexedProperty = typeof(string).GetProperties().First(p => (p.GetIndexParameters() ?? Array.Empty<ParameterInfo>()).Any());
				for (var valuesIndex = 0; valuesIndex < Values.Count; valuesIndex++)
				{
					var suffix = Values[valuesIndex];

					if (MatchType == MatchTypeOptions.WholeWord)
                    {
						// When matching whole words, just do a straight compare and normalise to the first option if it matches
						yield return (
							Expression.Equal(valueTrimmed, Expression.Constant(suffix)),
							Expression.Constant(Values[0])
						);
						continue;
					}

					var lengthCheck = Expression.GreaterThan(
						Expression.Property(valueTrimmed, lengthProperty),
						Expression.Constant(suffix.Length, typeof(int))
					);

					var conditionElements = NonNullImmutableList.Create(lengthCheck);

					for (var suffixCharacterIndex = 0; suffixCharacterIndex < suffix.Length; suffixCharacterIndex++)
					{
						conditionElements = conditionElements.Add(
							Expression.Equal(
								Expression.Constant(suffix[suffixCharacterIndex], typeof(char)),
								Expression.Property(
									valueTrimmed,
									indexedProperty,
									Expression.Subtract(
										Expression.Property(valueTrimmed, lengthProperty),
										Expression.Constant(suffix.Length - suffixCharacterIndex, typeof(int))
									)
								)
							)
						);
					}
					
					yield return (
						CombineExpressionsWithAndAlso(conditionElements),
						GenerateRemoveLastCharactersExpression(valueTrimmed, suffix.Length)
					);
				}
			}

			/// <summary>
			/// The value Expression must represent a non-null string that is as at least as long as the specified length or an exception will
			/// be thrown upon exection
			/// </summary>
			private static Expression GenerateRemoveLastCharactersExpression(Expression value, int length)
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));
				if (length < 0)
					throw new ArgumentOutOfRangeException(nameof(length));

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
		}

		[Serializable]
		public enum MatchTypeOptions
		{
			SuffixOnly,
			WholeWord
		}
	}
}