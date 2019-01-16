using System;
using FullTextIndexer.Core.Indexes.TernarySearchTree;
using Xunit;

namespace UnitTests.FullTextIndexer.Indexes.TernarySearchTree
{
    public class DefaultStringNormaliserTests
    {
        [Fact]
        public void NullWillThrowException()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                DefaultStringNormaliser.Instance.GetNormalisedString(null);
            });
        }

        [Fact]
        public void CatIsLowerCased()
        {
            Test("Cat", "cat");
        }

        [Fact]
        public void AcuteAccentRemovedOnMange()
        {
            Test("mangé", "mange");
        }

        private void Test(string valueToNormalise, string expected)
        {
            if (valueToNormalise == null)
                throw new ArgumentNullException("valueToNormalise");
            if (expected == null)
                throw new ArgumentNullException("expected");

            Assert.Equal(
                expected,
                (DefaultStringNormaliser.Instance).GetNormalisedString(valueToNormalise)
            );
        }
    }
}
