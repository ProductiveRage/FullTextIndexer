using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FullTextIndexer.Serialisation.Json
{
	/// <summary>
	/// For applicable types, this will use the serialised type name data that the EnhancedDetailContractResolver injects so that deserialisation attempts for interfaces
	/// (which JSON.NET can not handle) may be replaced with deserialisation attempts for concrete types (so that JSON.NET knows what class to deserialise to)
	/// </summary>
	public sealed class EnhancedDetailReadConverter : JsonConverter
	{
		private readonly string _typeNameProperty;
		private readonly Predicate<Type> _typeFilter;
		public EnhancedDetailReadConverter(string typeNameProperty, Predicate<Type> typeFilter)
		{
			if (string.IsNullOrWhiteSpace(typeNameProperty))
				throw new ArgumentException($"Null/blank {nameof(typeNameProperty)} specified");
			if (typeFilter == null)
				throw new ArgumentNullException(nameof(typeFilter));

			_typeNameProperty = typeNameProperty;
			_typeFilter = typeFilter;
		}

		public override bool CanConvert(Type objectType)
		{
			return _typeFilter(objectType);
		}

		public override bool CanRead { get { return true; } }
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			var data = JObject.Load(reader);
			var typeName = data[_typeNameProperty].ToObject<string>();
			var type = Type.GetType(typeName, throwOnError: true);
			return serializer.Deserialize(new JTokenReader(data), type);
		}

		public override bool CanWrite { get { return false; } }
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) { throw new NotImplementedException(); }
	}
}
