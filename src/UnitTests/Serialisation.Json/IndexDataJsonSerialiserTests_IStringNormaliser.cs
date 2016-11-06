using System;
using FullTextIndexer.Core.Indexes.TernarySearchTree;
using FullTextIndexer.Serialisation.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace UnitTests.Serialisation.Json
{
	public sealed class IndexDataJsonSerialiserTests_IStringNormaliser
	{
		/// <summary>
		/// Although the DefaultStringNormaliser has no configuration options, if it is being used as an IStringNormaliser then it is important that the type is maintained in the JSON so that
		/// the deserialisation process knows to produce a DefaultStringNormaliser instance and to not throw its toys out the pram when asked for an IStringNormaliser (ordinarily, JSON.NET
		/// can't deserialise to an interface since it has no idea what concrete type to use - since classes such as the TernarySearchTreeDictionary take an IStringNormaliser dependency, 
		/// it's important that the serialisation process be able to deal with it and so the IndexDataJsonSerialiser has special logic for IStringNormaliser).
		/// </summary>
		[Fact]
		public void DefaultStringNormaliserAsIStringNormaliser()
		{
			var clone = IndexDataJsonSerialiser.GenericSerialiser.Deserialise<IStringNormaliser>(
				IndexDataJsonSerialiser.GenericSerialiser.Serialise<IStringNormaliser>(
					new DefaultStringNormaliser()
				)
			);
			Assert.Equal(typeof(DefaultStringNormaliser), clone.GetType());
		}

		/// <summary>
		/// The EnglishPluralityStringNormaliser requires further logic on top of the IStringNormaliser serialisation logic, it needs to know how to serialise/deserialise private fields (but
		/// not all fields, since trying to serialise its internal compiled LINQ expression will not go well)
		/// </summary>
		[Fact]
		public void EnglishPluralityStringNormaliserWithDefaultOptionsAsIStringNormaliser()
		{
			var clone = IndexDataJsonSerialiser.GenericSerialiser.Deserialise<IStringNormaliser>(
				IndexDataJsonSerialiser.GenericSerialiser.Serialise<IStringNormaliser>(
					new EnglishPluralityStringNormaliser()
				)
			);
			Assert.Equal(typeof(EnglishPluralityStringNormaliser), clone.GetType());
		}

		/// <summary>
		/// This is an accompanient to EnglishPluralityStringNormaliserWithDefaultOptionsAsIStringNormaliser to ensure that the deserialisation process doesn't use the default constructor in
		/// all cases where an EnglishPluralityStringNormaliser is initialised
		/// </summary>
		[Fact]
		public void EnglishPluralityStringNormaliserWithCustomOptionsAsIStringNormaliser()
		{
			var clone = IndexDataJsonSerialiser.GenericSerialiser.Deserialise<IStringNormaliser>(
				IndexDataJsonSerialiser.GenericSerialiser.Serialise<IStringNormaliser>(
					new EnglishPluralityStringNormaliser(
						plurals: new[] { new EnglishPluralityStringNormaliser.PluralEntry(new[] { "eep", "eepzies" }, EnglishPluralityStringNormaliser.MatchTypeOptions.SuffixOnly) },
						optionalPreNormaliser: null,
						preNormaliserWork: EnglishPluralityStringNormaliser.PreNormaliserWorkOptions.PreNormaliserDoesNothing
					)
				)
			);
			Assert.Equal(typeof(EnglishPluralityStringNormaliser), clone.GetType());

			// To verify the configuration of the cloned string normaliser we could either use reflection to access the private field (yuck) or actually execute its code to ensure that it
			// returns the expected results (here, the word "sheepzies" - a fictional plurality - should be considered the same as "sheep")
			Assert.True(clone.Equals("sheep", "sheepzies"));
		}

		/// <summary>
		/// If custom string normalisers are used and they are simple for JSON.NET to serialise and deserialise then no complicated additional logic is required (in this case, the custom
		/// normaliser is immutable and has a single public property that corresponds precisely to the single argument on the only constructor)
		/// </summary>
		[Fact]
		public void CustomStringNormaliserWithPublicPropertiesAsIStringNormaliser()
		{
			var clone = IndexDataJsonSerialiser.GenericSerialiser.Deserialise<IStringNormaliser>(
				IndexDataJsonSerialiser.GenericSerialiser.Serialise<IStringNormaliser>(
					new PublicPropertyCaseEnforcingStringNormaliser(CasingOptions.UpperCase)
				)
			);
			Assert.Equal(typeof(PublicPropertyCaseEnforcingStringNormaliser), clone.GetType());
			Assert.Equal(CasingOptions.UpperCase, ((PublicPropertyCaseEnforcingStringNormaliser)clone).Casing);
		}

		/// <summary>
		/// This is similar CustomStringNormaliserWithPublicPropertiesAsIStringNormaliser except that the configuration is maintained internally using a private field instead of a public
		/// property (however, the special behaviour for dealing with IStringNormaliser implementations means that private field values are serialised and so no special serialisation logic
		/// is required here either)
		/// </summary>
		[Fact]
		public void CustomStringNormaliserWithPrivatePropertiesAsIStringNormaliser()
		{
			var clone = IndexDataJsonSerialiser.GenericSerialiser.Deserialise<IStringNormaliser>(
				IndexDataJsonSerialiser.GenericSerialiser.Serialise<IStringNormaliser>(
					new PrivatePropertyCaseEnforcingStringNormaliser(CasingOptions.UpperCase)
				)
			);
			Assert.Equal(typeof(PrivatePropertyCaseEnforcingStringNormaliser), clone.GetType());

			// To verify the configuration of the cloned string normaliser we could either use reflection to access the private field (yuck) or actually execute its code to ensure that it
			// returns the expected results (since we've configured it for UpperCase then passing a lower case string to its GetNormalisedString method should return an upper case version)
			Assert.Equal("ABC", clone.GetNormalisedString("abc"));
		}

		/// <summary>
		/// This custom IStringNormaliser has a private backing field which is of a different type to the constructor argument and so the a custom JsonConverter is required for deserialisation.
		/// The IStringNormaliser will have its private field serialised (this happens for all IStringNormaliser implementations) but some custom logic is required to translate that back into
		/// the form that the constructor expects - to do this, an additional JsonConvertere is passed to the Deserialise method call.
		/// </summary>
		[Fact]
		public void CustomStringNormaliserWithConfusinglyNamedPrivatePropertiesAsIStringNormaliser()
		{
			var clone = IndexDataJsonSerialiser.GenericSerialiser.Deserialise<IStringNormaliser>(
				IndexDataJsonSerialiser.GenericSerialiser.Serialise<IStringNormaliser>(
					new CustomStringNormaliserWithSillyConstructorArgument("UPPER")
				),
				CustomStringNormaliserWithSillyConstructorArgumentReadConverter.Instance
			);
			Assert.Equal(typeof(CustomStringNormaliserWithSillyConstructorArgument), clone.GetType());

			// To verify the configuration of the cloned string normaliser we could either use reflection to access the private field (yuck) or actually execute its code to ensure that it
			// returns the expected results (since we've configured it for UpperCase then passing a lower case string to its GetNormalisedString method should return an upper case version)
			// should return an upper case version)
			Assert.Equal("ABC", clone.GetNormalisedString("abc"));
		}

		private sealed class PublicPropertyCaseEnforcingStringNormaliser : IStringNormaliser
		{
			public PublicPropertyCaseEnforcingStringNormaliser(CasingOptions casing)
			{
				Casing = casing;
			}
			public CasingOptions Casing { get; }

			public string GetNormalisedString(string value)
			{
				return (Casing == CasingOptions.UpperCase) ? value.ToUpperInvariant() : value.ToLowerInvariant();
			}

			public bool Equals(string x, string y) { return GetNormalisedString(x) == GetNormalisedString(y); }
			public int GetHashCode(string obj) { return GetNormalisedString(obj).GetHashCode(); }
		}

		private sealed class PrivatePropertyCaseEnforcingStringNormaliser : IStringNormaliser
		{
			private CasingOptions _casing;
			public PrivatePropertyCaseEnforcingStringNormaliser(CasingOptions casing)
			{
				_casing = casing;
			}

			public string GetNormalisedString(string value)
			{
				return (_casing == CasingOptions.UpperCase) ? value.ToUpperInvariant() : value.ToLowerInvariant();
			}

			public bool Equals(string x, string y) { return GetNormalisedString(x) == GetNormalisedString(y); }
			public int GetHashCode(string obj) { return GetNormalisedString(obj).GetHashCode(); }
		}

		private sealed class CustomStringNormaliserWithSillyConstructorArgument : IStringNormaliser
		{
			private readonly CasingOptions _casing;
			public CustomStringNormaliserWithSillyConstructorArgument(string casing)
			{
				if (casing == "UPPER")
					_casing = CasingOptions.UpperCase;
				else if (casing == "lower")
					_casing = CasingOptions.UpperCase;
				else
					throw new ArgumentOutOfRangeException(nameof(casing));
			}

			public string GetNormalisedString(string value)
			{
				return (_casing == CasingOptions.UpperCase) ? value.ToUpperInvariant() : value.ToLowerInvariant();
			}

			public bool Equals(string x, string y) { return GetNormalisedString(x) == GetNormalisedString(y); }
			public int GetHashCode(string obj) { return GetNormalisedString(obj).GetHashCode(); }
		}

		private sealed class CustomStringNormaliserWithSillyConstructorArgumentReadConverter : JsonConverter
		{
			private static CustomStringNormaliserWithSillyConstructorArgumentReadConverter _instance = new CustomStringNormaliserWithSillyConstructorArgumentReadConverter();
			public static CustomStringNormaliserWithSillyConstructorArgumentReadConverter Instance => _instance;
			private CustomStringNormaliserWithSillyConstructorArgumentReadConverter() { }

			public override bool CanConvert(Type objectType)
			{
				return objectType == typeof(CustomStringNormaliserWithSillyConstructorArgument);
			}

			public override bool CanRead { get { return true; } }
			public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
			{
				var data = JObject.Load(reader);
				var casing = data["_casing"].ToObject<CasingOptions>();
				return new CustomStringNormaliserWithSillyConstructorArgument(
					(casing == CasingOptions.UpperCase) ? "UPPER" : "lower"
				);
			}

			public override bool CanWrite { get { return false; } }
			public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) { throw new NotImplementedException(); }
		}

		private enum CasingOptions { LowerCase, UpperCase }
	}
}
