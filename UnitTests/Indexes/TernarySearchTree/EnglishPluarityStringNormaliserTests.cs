using System;
using FullTextIndexer.Indexes.TernarySearchTree;
using Xunit;

namespace UnitTests.Indexes.TernarySearchTree
{
    public class EnglishPluarityStringNormaliserTests
    {
        [Fact]
        public void NullWillThrowException()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                (new EnglishPluralityStringNormaliser()).GetNormalisedString(null);
            });
        }

        [Fact]
        public void CatIsTransformedToCat_ses_es_s()
        {
            Test("Cat", "Cat[ses][es][s]");
        }

        [Fact]
        public void CatsIsTransformedToCat_ses_es_s()
        {
            Test("Cats", "Cat[ses][es][s]");
        }

        private void Test(string valueToNormalise, string expected)
        {
            if (valueToNormalise == null)
                throw new ArgumentNullException("valueToNormalise");
            if (expected == null)
                throw new ArgumentNullException("expected");

            Assert.Equal(
                expected,
                (new EnglishPluralityStringNormaliser()).GetNormalisedString(valueToNormalise)
            );
        }
    }
}
