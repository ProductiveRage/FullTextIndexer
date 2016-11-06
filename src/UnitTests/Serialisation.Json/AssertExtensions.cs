using System;
using System.Text.RegularExpressions;
using Xunit;

namespace UnitTests.Serialisation.Json
{
	public static class AssertExtensions
	{
		/// <summary>
		/// The IndexDataJsonSerialiser generates indented JSON but whitespace between properties is not important for the test, so it should be removed from both the generated and expected
		/// JSON strings. (It can make the test code neater if we don't have to worry about the indentations being space or tabs and if we can use more or less line returns in the to-test-for
		/// strings here). The IndexDataJsonSerialiser generates indented JSON for the rare occassion where it is useful to inspect it (it's likely to be pretty big anyway and so adding some
		/// more whitespace should be no big deal - if the size is an issue then it will easily compress down).
		/// </summary>
		public static void JsonEquivalent(string expectedJson, string json)
		{
			Assert.Equal(
				RemoveWhitespaceFromSerialisedJson(expectedJson),
				RemoveWhitespaceFromSerialisedJson(json)
			);
		}

		private static string RemoveWhitespaceFromSerialisedJson(string json)
		{
			if (string.IsNullOrWhiteSpace(json))
				throw new ArgumentException($"Null/blank {nameof(json)} specified");

			// Courtesy if http://stackoverflow.com/a/8913186/3813189
			return Regex.Replace(json, "(\"(?:[^\"\\\\]|\\\\.)*\")|\\s+", "$1");
		}
	}
}
