using FullTextIndexer.Common.Lists;
using FullTextIndexer.Serialisation.Json;
using Xunit;

namespace UnitTests.Serialisation.Json
{
	public sealed class IndexDataJsonSerialiserTests_NonNullImmutableList
	{
		/// <summary>
		/// Simple NonNullImmutableList instances should be represented by an array in JSON and basic immutable classes  (with non-ambiguous constructors that clearly correspond to available
		/// properties) should be represented by simple object literals
		/// </summary>
		[Fact]
		public void NonNullImmutableListIdAndNameItems()
		{
			var list = NonNullImmutableList.Create(
				new IdAndName(1, "Test1"),
				new IdAndName(2, "Test2")
			);
			var json = IndexDataJsonSerialiser.GenericSerialiser.Serialise(list);

			var expectedJson = @"
				[
					{ ""Id"": 1, ""Name"": ""Test1"" },
					{ ""Id"": 2, ""Name"": ""Test2"" }
				]
			";
			AssertExtensions.JsonEquivalent(expectedJson, json);

			var clone = IndexDataJsonSerialiser.GenericSerialiser.Deserialise<NonNullImmutableList<IdAndName>>(json);
			Assert.Equal(2, clone.Count);
			Assert.Equal(1, clone[0].Id);
			Assert.Equal("Test1", clone[0].Name);
			Assert.Equal(2, clone[1].Id);
			Assert.Equal("Test2", clone[1].Name);
		}

		private sealed class IdAndName
		{
			public IdAndName(int id, string name)
			{
				Id = id;
				Name = name;
			}
			public int Id { get; }
			public string Name { get; }
		}
	}
}
