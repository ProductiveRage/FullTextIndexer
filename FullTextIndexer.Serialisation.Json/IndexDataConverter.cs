using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FullTextIndexer.Common.Lists;
using FullTextIndexer.Core.Indexes;
using FullTextIndexer.Core.Indexes.TernarySearchTree;
using Newtonsoft.Json;

namespace FullTextIndexer.Serialisation.Json
{
	public sealed class IndexDataConverter : JsonConverter
	{
		private static readonly IndexDataConverter _instance = new IndexDataConverter();
		public static IndexDataConverter Instance => _instance;

		private delegate object Reader(JsonReader reader, JsonSerializer serialiser, Type elementType);
		private delegate void Writer(JsonWriter writer, object value, JsonSerializer serialiser, Type elementType);
		private readonly Reader _genericReaderMethodCaller;
		private readonly Writer _genericWriteMethodCaller;
		private IndexDataConverter()
		{
			var openGenericReadMethod = typeof(IndexDataConverter).GetMethod("Read", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod);
			_genericReaderMethodCaller = (reader, serialiser, elementType) =>
			{
				var genericGenericReadMethod = openGenericReadMethod.MakeGenericMethod(elementType);
				return genericGenericReadMethod.Invoke(this, new object[] { reader, serialiser });
			};

			var openGenericWriteMethod = typeof(IndexDataConverter).GetMethod("Write", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod);
			_genericWriteMethodCaller = (writer, value, serialiser, elementType) =>
			{
				var genericWriteJsonMethod = openGenericWriteMethod.MakeGenericMethod(elementType);
				genericWriteJsonMethod.Invoke(this, new object[] { writer, value, serialiser });
			};
		}

		public override bool CanConvert(Type objectType)
		{
			return objectType.IsConstructedGenericType && (objectType.GetGenericTypeDefinition() == typeof(IndexData<>));
		}

		public override bool CanRead { get { return true; } }
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			return _genericReaderMethodCaller(reader, serializer, objectType.GetGenericArguments().Single());
		}

		public override bool CanWrite { get { return true; } }
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			_genericWriteMethodCaller(writer, value, serializer, value.GetType().GetGenericArguments().Single());
		}

		private void Write<TKey>(JsonWriter writer, IndexData<TKey> value, JsonSerializer serializer)
		{
			// The IndexData<TKey> class doesn't provide direct public access to the TernarySearchTreeDictionary data so we need to get it using some reflection (possible
			// future improvement would be to either make it public - though that would extend its interface more than necessary, it would be nice to keep it as simple as
			// possible - or possibly to make the property internal and then allow access to this project to internals in Core.. would need to confirm that that is not
			// just some magic that work when the projects are all loaded side-by-side in Visual Studio and that it still works if they are loaded as separate NuGet
			// packages in another solution)
			var privateDataProperty = typeof(IndexData<TKey>).GetField("_data", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance);
			serializer.Serialize(
				writer,
				new SerialisableData<TKey>
				{
					MatchData = (TernarySearchTreeDictionary<NonNullImmutableList<WeightedEntry<TKey>>>)privateDataProperty.GetValue(value),
					DataKeyComparer = value.KeyComparer
				}
			);
		}

		private IndexData<TKey> Read<TKey>(JsonReader reader, JsonSerializer serializer)
		{
			var data = serializer.Deserialize<SerialisableData<TKey>>(reader);
			return new IndexData<TKey>(
				data.MatchData,
				data.DataKeyComparer
			);
		}

		private sealed class SerialisableData<TKey>
		{
			public TernarySearchTreeDictionary<NonNullImmutableList<WeightedEntry<TKey>>> MatchData { get; set; }
			public IEqualityComparer<TKey> DataKeyComparer { get; set; }
		}
	}
}
