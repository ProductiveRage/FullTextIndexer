using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Security;
using FullTextIndexer.Common.Lists;

namespace FullTextIndexer.Core.Indexes.TernarySearchTree
{
	/// <summary>
	/// This base class is designed to make it easier to normalise words by stemming them in some way, such as reducing plural versions of nouns to their singular
	/// form so that, for example, 'cat' and 'cats' are considered the same word when searching
	/// </summary>
	[Serializable]
	public abstract class StemmingStringNormaliser : StringNormaliser, ISerializable
	{
		private readonly NonNullImmutableList<IStemOpportunity> _stemOpportunities;
		private readonly Func<string, string> _normaliser; // Note: Don't try to serialise this, it's probably not possible (not via ISerializable nor JSON.Net)
		private readonly IStringNormaliser _optionalPreNormaliser;
		private readonly PreNormaliserWorkOptions _preNormaliserWork;
		public StemmingStringNormaliser(NonNullImmutableList<IStemOpportunity> stemOpportunities, IStringNormaliser optionalPreNormaliser, PreNormaliserWorkOptions preNormaliserWork)
		{
			if (stemOpportunities == null)
				throw new ArgumentNullException("stemOpportunities");
			var allPreNormaliserOptions = (PreNormaliserWorkOptions)0;
			foreach (PreNormaliserWorkOptions option in Enum.GetValues(typeof(PreNormaliserWorkOptions)))
				allPreNormaliserOptions = allPreNormaliserOptions | option;
			if ((preNormaliserWork & allPreNormaliserOptions) != preNormaliserWork)
				throw new ArgumentOutOfRangeException("preNormaliserWork");

			_stemOpportunities = stemOpportunities;
			_normaliser = GenerateNormaliser();
			_optionalPreNormaliser = optionalPreNormaliser;
			_preNormaliserWork = preNormaliserWork;
		}

		protected StemmingStringNormaliser(SerializationInfo info, StreamingContext context)
			: this(
				(NonNullImmutableList<IStemOpportunity>)info.GetValue("_stemOpportunities", typeof(NonNullImmutableList<IStemOpportunity>)),
				(IStringNormaliser)info.GetValue("_optionalPreNormaliser", typeof(IStringNormaliser)),
				(PreNormaliserWorkOptions)info.GetValue("_preNormaliserWork", typeof(PreNormaliserWorkOptions))
			) { }

		[SecurityCritical]
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			// Unfortunately we can't serialise the generated normaliser (we'll get a "Cannot serialize delegates over unmanaged function pointers, dynamic
			// methods or methods outside the delegate creator's assembly" error) so if we have to serialise this instance we'll store all of the data and
			// then re-generate the normaliser on de-serialisation. Not ideal from a performance point of view but at least it will work.
			info.AddValue("_stemOpportunities", _stemOpportunities);
			info.AddValue("_optionalPreNormaliser", _optionalPreNormaliser);
			info.AddValue("_preNormaliserWork", _preNormaliserWork);
		}

		[Serializable]
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
			foreach (var stemOpportunity in _stemOpportunities)
			{
				foreach (var (ifMatchPredicate, resultIfMatches) in stemOpportunity.GeneratePredicates(valueTrimmed))
				{
					// For each stem opportunity, there will be a predicate expression generated to determine whether it is applicable
					// and a normalised version of the string to use if it IS applicable (eg. a plurality normaliser may have a suffix
					// rule that means that "cats" becomes represented as "cat"). The "~" character is appended to the returned content
					// so that if a string is fed through the normalisation process multiple times then the return values will be stable
					// (eg. "cats" becomes "cat~" which stays at "cat~").
					expressions.Add(
						Expression.IfThen(
							ifMatchPredicate,
							Expression.Block(
								Expression.Assign(
									result,
									GenerateAppendStringExpression(resultIfMatches, "~")
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
					Expression.AndAlso(
						Expression.NotEqual(valueTrimmed, Expression.Constant("")),
						Expression.Call(valueTrimmed, typeof(string).GetMethod("EndsWith", new[] { typeof(string) }), Expression.Constant("~"))
					),
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

		protected static BinaryExpression CombineExpressionsWithAndAlso(NonNullImmutableList<BinaryExpression> expressions)
		{
			if (expressions == null)
				throw new ArgumentNullException("expressions");

			if (!expressions.Any())
				throw new Exception("No entries in expressions set");
			else if (expressions.Count == 1)
				return expressions[0];

			var reducedExpressions = NonNullImmutableList<BinaryExpression>.Empty;
			for (var index = 0; index < expressions.Count; index += 2)
			{
				var expression = expressions[index];
				if (index < (expressions.Count - 1))
				{
					var expressionNext = expressions[index + 1];
					reducedExpressions = reducedExpressions.Add(Expression.AndAlso(expression, expressionNext));
				}
				else
					reducedExpressions = reducedExpressions.Add(expression);
			}

			return (reducedExpressions.Count == 1) ? reducedExpressions[0] : CombineExpressionsWithAndAlso(reducedExpressions);
		}

		private static Expression GenerateAppendStringExpression(Expression value, string toAppend)
		{
			if (value == null)
				throw new ArgumentNullException("value");
			if (toAppend == null)
				throw new ArgumentNullException("toAppend");

			return Expression.Call(
				typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) }),
				value,
				Expression.Constant(toAppend)
			);
		}

		/// <summary>
		/// Given a set of values, ensure that none are null and return them de-duplicated after having been pushed through a string manipulation.
		/// This will throw an exception for null arguments or if any null value is encountered in the values set.
		/// </summary>
		protected static NonNullImmutableList<string> TidyStringList(NonNullImmutableList<string> values, Func<string, string> transformer)
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
			return valuesTidied.Distinct().ToNonNullImmutableList();
		}

		public interface IStemOpportunity
		{
			/// <summary>
			/// Generate a list of expression that determines whether a string parameter matches a specified IStemOpportunity - if any one of them
			/// indicate a match then it's a match (they don't ALL have to indicate a match) and, along with each BinaryExpression, there will be
			/// an expression to set the value to in order to be represented in its 'normalised' form
			/// </summary>
			IEnumerable<(BinaryExpression IsMatch, Expression ReduceToIfMatch)> GeneratePredicates(ParameterExpression valueTrimmed);
		}
	}
}