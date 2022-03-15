using System;
using FullTextIndexer.Common.Lists;
using FullTextIndexer.Core.Indexes.TernarySearchTree;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FullTextIndexer.Serialisation.Json
{
	/// <summary>
	/// This will only work if the normaliser was serialised with the EnhancedDetailContractResolver since private field data is required
	/// </summary>
	public sealed class EnglishPluralityStringNormaliserReadConverter : JsonConverter
	{
		private static EnglishPluralityStringNormaliserReadConverter _instance = new EnglishPluralityStringNormaliserReadConverter();
		public static EnglishPluralityStringNormaliserReadConverter Instance => _instance;
		private EnglishPluralityStringNormaliserReadConverter() { }

		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(EnglishPluralityStringNormaliser);
		}

		public override bool CanRead { get { return true; } }
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			var data = JObject.Load(reader);
			var plurals = data["_plurals"].ToObject<NonNullImmutableList<EnglishPluralityStringNormaliser.PluralEntry>>();
			var preNormaliserWork = data["_preNormaliserWork"].ToObject<StemmingStringNormaliser.PreNormaliserWorkOptions>();
			var optionalPreNormaliserData = data["_optionalPreNormaliser"];
			var optionalPreNormaliser = (optionalPreNormaliserData.Type == JTokenType.Null)
				? null
				: serializer.Deserialize<IStringNormaliser>(new JTokenReader(optionalPreNormaliserData));
			return new EnglishPluralityStringNormaliser(plurals, optionalPreNormaliser, preNormaliserWork);
		}

		public override bool CanWrite { get { return false; } }
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) { throw new NotImplementedException(); }
	}
}
