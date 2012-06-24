using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Blog.Models;
using Common.Lists;
using Common.Logging;
using FullTextIndexer;
using FullTextIndexer.Indexes;
using FullTextIndexer.Indexes.TernarySearchTree;
using FullTextIndexer.IndexGenerators;
using FullTextIndexer.TokenBreaking;

namespace Tester
{
    public class BlogExample
    {
        private ITokenBreaker _tokenBreaker;
        private IStringNormaliser _sourceStringComparer;
        private ILogger _logger;
        public BlogExample(ITokenBreaker tokenBreaker, IStringNormaliser sourceStringComparer, ILogger logger)
        {
            if (tokenBreaker == null)
                throw new ArgumentNullException("tokenBreaker");
            if (sourceStringComparer == null)
                throw new ArgumentNullException("sourceStringComparer");
            if (logger == null)
                throw new ArgumentNullException("logger");

            _tokenBreaker = tokenBreaker;
            _sourceStringComparer = sourceStringComparer;
            _logger = logger;
        }

        public void Go()
        {
            var postRepository = new AppDataTextPostRepository(
                new SingleFolderPostRetriever(
                    new DirectoryInfo(@"F:\Documents and Settings\Dan\My Documents\Dropbox\Blog\Blog\App_Data\Posts")
                )
            );
            var index = Generate(
                postRepository.GetAll().ToNonNullImmutableList()
            );

            var a = index.GetAllTokens().ToArray();

            var a1 = index.GetMatches("i");
            var a2 = index.GetMatches("immutable");
            var a3 = index.GetMatches("nonnullimmutable");
            var a4 = index.GetMatches("nonnullimmutablelist");
            var a5 = index.GetMatches("NonNullImmutableList<AbstractFormElement>");
            var a6 = index.GetMatches("NonNullImmutableList<AbstractFormElement>()");
        }

        public IIndexData<int> Generate(NonNullImmutableList<Post> data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            var contentRetrievers = new List<ContentRetriever<Post, int>>();
            contentRetrievers.Add(new ContentRetriever<Post, int>(
                p => new PreBrokenContent<int>(p.Id, p.Title),
                GetTokenWeightDeterminer(5f)
            ));
            contentRetrievers.Add(new ContentRetriever<Post, int>(
                p => new PreBrokenContent<int>(p.Id, p.HtmlContent),
                GetTokenWeightDeterminer(1f)
            ));
            contentRetrievers.Add(new ContentRetriever<Post, int>(
                // TODO: Can the tags be processed individually instead of combining into a single string to break?
                p => !p.Tags.Any() ? null : new PreBrokenContent<int>(p.Id, string.Join(" ", p.Tags)),
                GetTokenWeightDeterminer(3f)
            ));

            return new IndexGenerator<Post, int>(
                contentRetrievers.ToNonNullImmutableList(),
                new IntEqualityComparer(),
                _sourceStringComparer,
                _tokenBreaker,
                weightedValues => weightedValues.Sum(),
                _logger
            ).Generate(data.ToNonNullImmutableList());
        }

        private ContentRetriever<Post, int>.BrokenTokenWeightDeterminer GetTokenWeightDeterminer(float multiplier)
        {
            if (multiplier <= 0)
                throw new ArgumentOutOfRangeException("multiplier", "must be greater than zero");
            return token => multiplier * (Constants.GetStopWords("en").Contains(token, _sourceStringComparer) ? 0.01f : 1f);
        }

        [Serializable]
        private class IntEqualityComparer : IEqualityComparer<int>
        {
            public bool Equals(int x, int y)
            {
                return (x == y);
            }
            public int GetHashCode(int obj)
            {
                return obj;
            }
        }
    }
}
