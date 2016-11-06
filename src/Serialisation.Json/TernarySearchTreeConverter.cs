using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FullTextIndexer.Core.Indexes.TernarySearchTree;
using Newtonsoft.Json;

namespace FullTextIndexer.Serialisation.Json
{
	public sealed class TernarySearchTreeConverter : JsonConverter
	{
		private static TernarySearchTreeConverter _instance = new TernarySearchTreeConverter();
		public static TernarySearchTreeConverter Instance => _instance;

		private delegate object Reader(JsonReader reader, JsonSerializer serialiser, Type elementType);
		private delegate void Writer(JsonWriter writer, object value, JsonSerializer serialiser, Type elementType);
		private readonly Reader _genericReaderMethodCaller;
		private readonly Writer _genericWriteMethodCaller;
		private TernarySearchTreeConverter()
		{
			var openGenericReadMethod = typeof(TernarySearchTreeConverter).GetMethod("Read", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod);
			_genericReaderMethodCaller = (reader, serialiser, elementType) =>
			{
				var genericGenericReadMethod = openGenericReadMethod.MakeGenericMethod(elementType);
				return genericGenericReadMethod.Invoke(this, new object[] { reader, serialiser });
			};

			var openGenericWriteMethod = typeof(TernarySearchTreeConverter).GetMethod("Write", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod);
			_genericWriteMethodCaller = (writer, value, serialiser, elementType) =>
			{
				var genericWriteJsonMethod = openGenericWriteMethod.MakeGenericMethod(elementType);
				genericWriteJsonMethod.Invoke(this, new object[] { writer, value, serialiser });
			};
		}

		public override bool CanConvert(Type objectType)
		{
			return objectType.IsConstructedGenericType && (objectType.GetGenericTypeDefinition() == typeof(TernarySearchTreeDictionary<>));
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

		private void Write<TValue>(JsonWriter writer, TernarySearchTreeDictionary<TValue> value, JsonSerializer serializer)
		{
			// Note: Note using the ToDictionary method since that uses the KeyNormaliser as the IEqualityComparer on the dictionary, which is just one more complication - it's
			// simpler to just take lists of the keys and value and then to recombine when de-serialising
			serializer.Serialize(
				writer,
				new SerialisableData<TValue>
				{
					NormalisedKeysWithValues = value.GetAllNormalisedKeys().Zip(
						value.GetAllValues(),
						(normalisedKey, matches) => new KeyValuePair<string, TValue>(normalisedKey, matches)
					),
					KeyNormaliser = value.KeyNormaliser
				}
			);
		}

		private TernarySearchTreeDictionary<TValue> Read<TValue>(JsonReader reader, JsonSerializer serializer)
		{
			var data = serializer.Deserialize<SerialisableData<TValue>>(reader);
			return new TernarySearchTreeDictionary<TValue>(
				data.NormalisedKeysWithValues,
				data.KeyNormaliser
			);
		}

		private sealed class SerialisableData<TValue>
		{
			public IEnumerable<KeyValuePair<string, TValue>> NormalisedKeysWithValues { get; set; }
			public IStringNormaliser KeyNormaliser { get; set; }
		}
	}
}
