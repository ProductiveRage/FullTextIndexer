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
            TestSpecificTransformation("cat", "cat|ses|es|s");
        }

        [Fact]
        public void CatsIsTransformedToCat_ses_es_s()
        {
            TestSpecificTransformation("cats", "cat|ses|es|s");
        }

        [Fact]
        public void EnsureMatch_cat_cats()
        {
            TestMatch("cat", "cats");
        }

        [Fact]
        public void EnsureMatch_category_categories()
        {
            TestMatch("category", "categories");
        }

        [Fact]
        public void EnsureMatch_child_children()
        {
            TestMatch("child", "children");
        }

        [Fact]
        public void EnsureMatch_medium_media()
        {
            TestMatch("medium", "media");
        }

        [Fact]
        public void EnsureMatch_cactus_cactii()
        {
            TestMatch("cactus", "cactii");
        }

        [Fact]
        public void EnsureMatch_formula_formulae()
        {
            TestMatch("formula", "formulae");
        }

        [Fact]
        public void EnsureMatch_formula_formulas()
        {
            TestMatch("formula", "formulas");
        }

        [Fact]
        public void EnsureMatch_index_indexes()
        {
            TestMatch("index", "indexes");
        }

        [Fact]
        public void EnsureMatch_index_indices()
        {
            TestMatch("index", "indices");
        }

        [Fact]
        public void EnsureMatch_matrix_matrices()
        {
            TestMatch("matrix", "matrices");
        }

        [Fact]
        public void EnsureMatch_vertex_vertices()
        {
            TestMatch("vertex", "vertices");

        }

        [Fact]
        public void EnsureMatch_datum_data()
        {
            TestMatch("datum", "data");

        }
        [Fact]
        public void EnsureMatch_man_men()
        {
            TestMatch("man", "men");

        }

        [Fact]
        public void EnsureMatch_woman_women()
        {
            TestMatch("woman", "women");
    
        }

        [Fact]
        public void EnsureMatch_one_ones()
        {
            TestMatch("one", "ones");
        }

        [Fact]
        public void EnsureMatch_rune_runes()
        {
            TestMatch("rune", "runes");
        }

        [Fact]
        public void EnsureMatch_tome_tomes()
        {
            TestMatch("tome", "tomes");
        }

        private void TestSpecificTransformation(string valueToNormalise, string expected)
        {
            if (valueToNormalise == null)
                throw new ArgumentNullException("valueToNormalise");
            if (expected == null)
                throw new ArgumentNullException("expected");

            Assert.Equal(
                expected,
                GetNormaliser().GetNormalisedString(valueToNormalise)
            );
        }

        private void TestMatch(string value1, string value2)
        {
            if (value1 == null)
                throw new ArgumentNullException("value1");
            if (value2 == null)
                throw new ArgumentNullException("value2");

            Assert.True(
                GetNormaliser().Equals(value1, value2)
            );
        }

        private IStringNormaliser GetNormaliser()
        {
            return new EnglishPluralityStringNormaliser();
        }
    }
}
