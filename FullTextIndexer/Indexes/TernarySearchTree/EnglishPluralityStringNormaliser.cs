using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace FullTextIndexer.Indexes.TernarySearchTree
{
    /// <summary>
    /// This will match common strings where one is the plural and the other the singular version of the same word. It not intended to be perfect and may
    /// match a few false positives, but it should catch most of the most common cases.
    /// </summary>
    [Serializable]
    public class EnglishPluralityStringNormaliser : StringNormaliser
    {
        private Func<string, string> _normaliser;
        private IStringNormaliser _optionalPreNormaliser;
        private PreNormaliserWorkOptions _preNormaliserWork;
        public EnglishPluralityStringNormaliser(
            IEnumerable<PluralEntry> plurals,
            IEnumerable<string> fallbackSuffixes,
            IStringNormaliser optionalPreNormaliser,
            PreNormaliserWorkOptions preNormaliserWork)
        {
            if (plurals == null)
                throw new ArgumentNullException("pluralEntries");
            if (fallbackSuffixes == null)
                throw new ArgumentNullException("fallbackSuffixes");
            var allPreNormaliserOptions = (PreNormaliserWorkOptions)0;
            foreach (PreNormaliserWorkOptions option in Enum.GetValues(typeof(PreNormaliserWorkOptions)))
                allPreNormaliserOptions = allPreNormaliserOptions | option;
            if ((preNormaliserWork & allPreNormaliserOptions) != preNormaliserWork)
                throw new ArgumentOutOfRangeException("preNormaliserWork");

            _normaliser = GenerateNormaliser(plurals, fallbackSuffixes);
            _optionalPreNormaliser = optionalPreNormaliser;
            _preNormaliserWork = preNormaliserWork;
        }

        public EnglishPluralityStringNormaliser(IStringNormaliser optionalPreNormaliser, PreNormaliserWorkOptions preNormaliserWork)
            : this(DefaultPlurals, DefaultFallback, optionalPreNormaliser, preNormaliserWork) { }
        
        public EnglishPluralityStringNormaliser() : this(null, PreNormaliserWorkOptions.PreNormaliserDoesNothing) { }

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

        private static Func<string, string> GenerateNormaliser(IEnumerable<PluralEntry> plurals, IEnumerable<string> fallbackSuffixes)
        {
            if (plurals == null)
                throw new ArgumentNullException("pluralEntries");
            if (fallbackSuffixes == null)
                throw new ArgumentNullException("fallbackSuffixes");

            // Build up if statements for each suffix - if a match is found, return the input value with the matched suffix replaced
            // with a combination of all the other suffixes in PluralEntry
            var result = Expression.Parameter(typeof(string), "result");
            var endLabel = Expression.Label(typeof(string));
            var valueTrimmed = Expression.Parameter(typeof(string), "valueTrimmed");
            var expressions = new List<Expression>();
            foreach (var plural in plurals)
            {
                if (plural == null)
                    throw new ArgumentException("Null reference encountered in plurals set");

                foreach (var suffix in plural.Values)
                {
                    expressions.Add(
                        Expression.IfThen(
                            GeneratePredicate(suffix, valueTrimmed, plural.MatchType),
                            Expression.Block(
                                Expression.Assign(
                                    result,
                                    GenerateStringConcatExpression(
                                        GenerateRemoveLastCharactersExpression(valueTrimmed, suffix.Length),
                                        Expression.Constant(CreateSuffixExtension(plural.Values), typeof(string))
                                    )
                                ),
                                Expression.Return(endLabel, result)
                            )
                        )
                    );
                }
            }

            // If any fallback suffixes are specified, add a statement to append them if none of the PluralEntry matches are made
            fallbackSuffixes = TidyStringList(fallbackSuffixes, v => v.Trim().ToLower());
            if (fallbackSuffixes.Any())
            {
                expressions.Add(
                    Expression.Assign(
                        result,
                        GenerateStringConcatExpression(
                            valueTrimmed,
                            Expression.Constant(CreateSuffixExtension(fallbackSuffixes), typeof(string))
                        )
                    )
                );
            }
            else
                expressions.Add(Expression.Assign(result, valueTrimmed));

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

        /// <summary>
        /// The values Expressions must represent strings otherwise the expression will fail when executed
        /// </summary>
        private static Expression GenerateStringConcatExpression(params Expression[] values)
        {
            if (values == null)
                throw new ArgumentNullException("values");

            var valuesTidied = values.ToList();
            if (!valuesTidied.Any())
                throw new ArgumentException("No entries in values set");
            if (valuesTidied.Any(v => v == null))
                throw new ArgumentException("Null reference encountered in values set");

            return Expression.Call(
                typeof(string).GetMethod("Concat", new[] { typeof(string[]) }),
                Expression.NewArrayInit(
                    typeof(string),
                    valuesTidied
                )
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
        private static IEnumerable<string> TidyStringList(IEnumerable<string> values, Func<string, string> transformer)
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
            return valuesTidied.Distinct();
        }

        public readonly static IEnumerable<string> DefaultFallback = new[] { "ses", "es", "s" };
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

        [Serializable]
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

                Values = valuesTidied.Distinct().ToList().AsReadOnly();
                MatchType = matchType;
            }

            /// <summary>
            /// This will never be null or an empty set, nor will it contain any null, empty or duplicate values (all values are lower-cased and trimmed)
            /// </summary>
            public ReadOnlyCollection<string> Values { get; private set; }

            public MatchTypeOptions MatchType { get; private set; }
        }

        [Serializable]
        public enum MatchTypeOptions
        {
            SuffixOnly,
            WholeWord
        }
    }
}
