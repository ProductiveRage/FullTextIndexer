using System.Collections.Generic;
using System.Linq;
using FullTextIndexer.Common.Lists;
using FullTextIndexer.Core.Indexes.TernarySearchTree;
using FullTextIndexer.Serialisation.Json;
using Xunit;

namespace UnitTests.Serialisation.Json
{
	public sealed class IndexDataJsonSerialiserTests_TernarySearchTree
	{
		/// <summary>
		/// Maybe this test bleeds over into testing the functionality of the TernarySearchTreeDictionary but it should also do a reasonable job of confirming that the serialisation process
		/// maintains all of the data
		/// </summary>
		[Fact]
		public void SimpleEndToEnd()
		{
			var data = new Dictionary<string, int>
			{
				{ "a", 1 },
				{ "b", 3 },
				{ "ab", 2 },
				{ "abd", 1 },
				{ "bde", 4 },
				{ "cde", 5 },
				{ "ff", 2 },
			};

			var clone = IndexDataJsonSerialiser.GenericSerialiser.Deserialise<TernarySearchTreeDictionary<int>>(
				IndexDataJsonSerialiser.GenericSerialiser.Serialise(
					new TernarySearchTreeDictionary<int>(
						data,
						DefaultStringNormaliser.Instance
					)
				)
			);
			Assert.Equal(typeof(TernarySearchTreeDictionary<int>), clone.GetType());
			Assert.Equal(data.Count, clone.GetAllNormalisedKeys().Count());
			foreach (var keyAndValue in data)
				Assert.Equal(keyAndValue.Value, clone[keyAndValue.Key]);
		}
	}
}