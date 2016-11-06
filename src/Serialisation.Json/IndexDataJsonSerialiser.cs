using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FullTextIndexer.Core.Indexes;
using FullTextIndexer.Core.Indexes.TernarySearchTree;
using Newtonsoft.Json;

namespace FullTextIndexer.Serialisation.Json
{
	public static class IndexDataJsonSerialiser
	{
		/// <summary>
		/// This will never return null, it will raise an exception if unable to satisfy the request (including the cases of null source or stream references)
		/// </summary>
		public static void Serialise<TKey>(IndexData<TKey> source, Stream stream, params JsonConverter[] additionalConverters)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));

			using (var writer = new StreamWriter(stream))
			{
				using (var jsonWriter = new JsonTextWriter(writer))
				{
					GetSerialiser(additionalConverters).Serialize(jsonWriter, source);
				}
			}
		}

		/// <summary>
		/// This will never return null, it will raise an exception if unable to satisfy the request (including the cases of a null stream reference)
		/// </summary>
		public static IndexData<TKey> Deserialise<TKey>(Stream stream, params JsonConverter[] additionalConverters)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));

			using (var reader = new StreamReader(stream))
			{
				using (var jsonReader = new JsonTextReader(reader))
				{
					return GetSerialiser(additionalConverters).Deserialize<IndexData<TKey>>(jsonReader);
				}
			}
		}

		/// <summary>
		/// TODO: Explain
		/// </summary>
		internal static class GenericSerialiser
		{
			public static string Serialise<T>(T source, params JsonConverter[] additionalConverters)
			{
				if (source == null)
					throw new ArgumentNullException(nameof(source));

				using (var writer = new StringWriter())
				{
					GetSerialiser(additionalConverters).Serialize(writer, source);
					return writer.ToString();
				}
			}

			public static T Deserialise<T>(string json, params JsonConverter[] additionalConverters)
			{
				if (string.IsNullOrWhiteSpace(json))
					throw new ArgumentException($"Null/blank {nameof(json)} specified");

				using (var reader = new StringReader(json))
				{
					using (var jsonReader = new JsonTextReader(reader))
					{
						return GetSerialiser(additionalConverters).Deserialize<T>(jsonReader);
					}
				}
			}
		}
		private static JsonSerializer GetSerialiser(JsonConverter[] additionalConverters)
		{
			if (additionalConverters == null)
				throw new ArgumentNullException(nameof(additionalConverters));

			const string typeNameProperty = "$$type";
			var serialiser = new JsonSerializer
			{
				ContractResolver = new EnhancedDetailContractResolver(
					typeNameProperty,
					type =>
						// If the type is a (concrete..?) implementation of IStringNormaliser or IEqualityComparer<> then we'll need extra data to correctly deserialise
						// Note: GetInterfaces returns ALL implemented interfaces, not just those directly implemented - which is good because it prevents any hassles with
						// recursively trying to determine what interfaces are directly and indirectly implemented
						typeof(IStringNormaliser).IsAssignableFrom(type) ||
						type.GetInterfaces().Any(i => i.IsConstructedGenericType && i.GetGenericTypeDefinition() == typeof(IEqualityComparer<>))
				),
				Formatting = Formatting.Indented,
				NullValueHandling = NullValueHandling.Include
			};
			serialiser.Converters.Add(
				new EnhancedDetailReadConverter(
					typeNameProperty,
					type =>
						// If deserialising a IStringNormaliser or IEqualityComparer<> interface then we'll need to extract the extra detail - this will allow us to
						// deserialise into the final type, rather than trying to deserialise to an interface
						(type == typeof(IStringNormaliser)) ||
						(type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(IEqualityComparer<>))
				)
			);
			serialiser.Converters.Add(EnglishPluralityStringNormaliserReadConverter.Instance);
			serialiser.Converters.Add(TernarySearchTreeConverter.Instance);
			serialiser.Converters.Add(IndexDataConverter.Instance);
			foreach (var additionalConverter in additionalConverters)
			{
				if (additionalConverter == null)
					throw new ArgumentException($"Null reference encountered in {nameof(additionalConverters)} set");
				serialiser.Converters.Add(additionalConverter);
			}
			return serialiser;
		}
	}
}
