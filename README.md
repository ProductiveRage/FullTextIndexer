# Full Text Indexer

[![NuGet version (FullTextIndexer)](https://img.shields.io/nuget/v/FullTextIndexer.svg?style=flat-square)](https://www.nuget.org/packages/FullTextIndexer)

*(If you happen to be curious about how any of this works under the hood, I've written a series of blog posts about its various aspects - see [The Full Text Indexer Post Round-up](http://www.productiverage.com/the-full-text-indexer-post-roundup))*

This is a project to play around with some ideas for a Full-Text Indexer. It was inspired by some problems I encountered with a Lucene integration. It's not intended to be any kind of drop-in replacement or compete on performance necessarily, but to follow the train of thought "how hard can it be??"

An "Index Generator" must be defined which takes a set of keyed data and generates an index that maps tokens generated from the content onto keys, along with a match weight. In the simplest case, tokens are individual words in strings in the input data. While the most obvious case for token matching is to do a straight comparison of search term to tokens in the generated index there is also support for common variations such as case-insensitivity, ignoring of punctuation, plurality handling (in English) and facility to match individual terms in the query to produce a combined match weight (rather than requiring that the entire query match, for cases where a multiple-word query is specified).

The Index Generator can assign different weights to tokens extracted from different fields (in the below example tokens extracted from Post Title values are given a greater weight than tokens extracted from Post Content) and per-token weight multipliers can be applied (the below example reduces the weight of tokens that are common English "stop words" - such as "a", "and", the", etc..).

There are some interesting implementation details incorporated for performance - the TernarySearchTreeDictionary used for token lookups in the generated Index instances, the singly-linked-list persistent ImmutableList and the compiled LINQ Expressions in the EnglishPluralityStringNormaliser.

## The example

The following will generate an index from a list of Posts. The manner in which tokens are extracted from the data is specified in the GetPostIndexGenerator method; greater weight is given to tokens extracted from the Title than the Content property, weight is greatly reduced for English stop words in either property, an EnglishPluralityStringNormaliser is specified as the index search comparer, input strings are split on whitespace and common word-break punctuation (such as brackets and  commas) and where the same token is identified multiple times for the same Post the weights will be combined in an additive manner.

Some simple data is pushed through the generator and then a query performed on that index. Using the "GetPartialMatches" method breaks the query term "cat posts" into separate searches for "cat" and "posts" and combines the results - the third argument to GetPartialMatches (the "matchCombiner" lambda) specifies that only Posts that match all of the separate terms (both "cat" *and* "post") may be elligible to be returned in the results. In the example, both Posts are both found to match though Post 2 gets a higher weight as it matches "cat" twice (to "cats" and "Cats") and "posts" once (to "post") while Post 1 matches "cat" once and "posts" once.

    using System;
    using System.Linq;
    using FullTextIndexer.Common.Lists;
    using FullTextIndexer.Common.Logging;
    using FullTextIndexer.Core.Indexes;
    using FullTextIndexer.Core.Indexes.TernarySearchTree;
    using FullTextIndexer.Core.IndexGenerators;
    using FullTextIndexer.Core.TokenBreaking;

    namespace Tester
    {
        class Program
        {
            static void Main()
            {
                var indexGenerator = GetPostIndexGenerator();
                var index = indexGenerator.Generate(NonNullImmutableList.Create(
                    new Post(1, "One", "This is a post about a cat."),
                    new Post(2, "Two", "A follow-up post, also about cats. Cats are the best."),
                    new Post(3, "Three", "Today I talk about dogs.")));

                var catPosts = index
                    .GetPartialMatches(
                        "cat posts",
                        GetTokenBreaker(),
                        (tokenMatches, allTokens) => (tokenMatches.Count < allTokens.Count)
							? 0
							: tokenMatches.Sum(m => m.Weight))
                    .OrderByDescending(match => match.Weight);

                Console.WriteLine("Results:");
                Console.WriteLine();
                foreach (var searchResult in catPosts)
                    Console.WriteLine($" Post id {searchResult.Key}, match weight {searchResult.Weight}");
            }

            private static IndexGenerator<Post, int> GetPostIndexGenerator()
            {
                var sourceStringComparer = new EnglishPluralityStringNormaliser(
                    DefaultStringNormaliser.Instance,
                    EnglishPluralityStringNormaliser.PreNormaliserWorkOptions.PreNormaliserLowerCases
                        | EnglishPluralityStringNormaliser.PreNormaliserWorkOptions.PreNormaliserTrims);

                // Define the manner in which the raw content is retrieved from Post
                // title and body
                // - English stop words will only receive 1% the weight when match
                //     qualities are determined than other words will receive
                // - Words in the title will be given 5x the weight of words found
                //     in body content
                var stopWords = FullTextIndexer.Core.Constants.GetStopWords("en");
                var contentRetrievers = NonNullImmutableList.Create(
                    new ContentRetriever<Post, int>(
                        p => new PreBrokenContent<int>(p.Id, p.Title),
                        token => (stopWords.Contains(token, sourceStringComparer) ? 0.01f : 1f) * 5f),
                    new ContentRetriever<Post, int>(
                        p => new PreBrokenContent<int>(p.Id, p.Content),
                        token => stopWords.Contains(token, sourceStringComparer) ? 0.01f : 1f));

                // Generate an index using the specified StringNormaliser, 
                // - The Post class has an integer Id so the DefaultEqualityComparer will
                //     do the job just fine for the dataKeyComparer
                // - If the search term is matched multiple times in a Post then combine
                //     the match weight in a simple additive manner (hence the
                //     weightedValues.Sum() call)
                // Note: When "captureSourceLocations" is enabled, data about where tokens
                // were matched in the source data is returned, which allows highlighting
                // of matched terms (this code doesn't demonstrate that but it is possible!)
                return new IndexGenerator<Post, int>(
                    contentRetrievers,
                    new DefaultEqualityComparer<int>(),
                    sourceStringComparer,
                    GetTokenBreaker(),
                    weightedValues => weightedValues.Sum(),
                    captureSourceLocations: true,
                    new NullLogger());
            }

            private static ITokenBreaker GetTokenBreaker()
            {
                // Specify the token breaker
                // - English content will generally break on "." and "," (unlike "'" or
                //     "-" which are commonly part of words). Also break on round brackets
                //     for written content but also the other bracket types and other
                //     common characters that might represent word breaks in code
                return new WhiteSpaceExtendingTokenBreaker(
                    ImmutableList.Create(
                        '<', '>', '[', ']', '(', ')', '{', '}',
                        '.', ',', ':', ';', '"', '?', '!',
                        '/', '\\',
                        '@', '+', '|', '='),
                    new WhiteSpaceTokenBreaker());
            }

            public class Post
            {
                public Post(int id, string title, string content)
                {
                    Id = id;
                    Title = string.IsNullOrWhiteSpace(title) ? throw new ArgumentException("Null/blank title specified") : title.Trim();
                    Content = string.IsNullOrWhiteSpace(content) ? throw new ArgumentException("Null/blank content specified") : content.Trim();
                }
                public int Id { get; }
                public string Title { get; }
                public string Content { get; }
            }
        }
    }

More information about this project - some of its approaches, some of the implementation details, some alternate ways to configure the IndexGenerator and somewhere it's actually used! - can be found at my [Full Text Indexer Round-up](http://www.productiverage.com/Read/40) blog post.

## The example: Trimmed using Automated Index Generation

The above code illustrates how to configure all of the options for Index Generation but there is a class in the FullTextIndexer.Helpers namespace that can do a lot of the hard work by examining the source type with reflection and liberally applying defaults. The resulting code (which enables generation of a searchable index in just a few lines) is:

    using System;
    using System.Linq;
    using FullTextIndexer.Common.Lists;
    using FullTextIndexer.Core.Indexes;

    namespace Tester
    {
        class Program
        {
            static void Main(string[] args)
            {
                var indexGenerator = new AutomatedIndexGeneratorFactoryBuilder<Post, int>()
                    .SetWeightMultiplier(typeof(Post).GetProperty("Title"), 5)
                    .Get()
                    .Get();

                var index = indexGenerator.Generate(NonNullImmutableList.Create(
                    new Post(1, "One", "This is a post about a cat."),
                    new Post(2, "Two", "A follow-up post, also about cats. Cats are the best."),
                    new Post(3, "Three", "Today I talk about dogs.")));

                var catPosts = index
                    .GetPartialMatches("cat posts")
                    .OrderByDescending(match => match.Weight);

                Console.WriteLine("Results:");
                Console.WriteLine();
                foreach (var searchResult in catPosts)
                    Console.WriteLine($" Post id {searchResult.Key}, match weight {searchResult.Weight}");
            }

            public class Post
            {
                public Post(int id, string title, string content)
                {
                    Id = id;
                    Title = string.IsNullOrWhiteSpace(title) ? throw new ArgumentException("Null/blank title specified") : title.Trim();
                    Content = string.IsNullOrWhiteSpace(content) ? throw new ArgumentException("Null/blank content specified") : content.Trim();
                }
                public int Id { get; }
                public string Title { get; }
                public string Content { get; }
            }
        }
    }

More information can be found about this in the Post [The Full Text Indexer - Automating Index Generation](http://www.productiverage.com/Read/48).
