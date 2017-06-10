using System.Collections.Generic;
using FullTextIndexer.Core.IndexGenerators;
using FullTextIndexer.Serialisation.Json;
using Xunit;

namespace UnitTests.Serialisation.Json
{
	public sealed class IndexDataJsonSerialiserTests_IEqualityComparear
	{
		/// <summary>
		/// Since the IndexData class take a generic IEqualityComparer dependency, the (de)serialisation process needs to be able to reconstruct the original type when all it knows is that the
		/// destination type is a generic IEqualityComparer interface (ordinarily, JSON.NET can't deserialise to an interface since it has no idea what concrete type to use)
		/// </summary>
		[Fact]
		public void DefaultIntEqualityComparerAsIEqualityComparer()
		{
			var clone = IndexDataJsonSerialiser.GenericSerialiser.Deserialise<IEqualityComparer<int>>(
				IndexDataJsonSerialiser.GenericSerialiser.Serialise<IEqualityComparer<int>>(
					new DefaultEqualityComparer<int>()
				)
			);
			Assert.Equal(typeof(DefaultEqualityComparer<int>), clone.GetType());
		}
	}
}
