using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using FullTextIndexer.Common.Lists;
using FullTextIndexer.Core.Indexes.TernarySearchTree;

namespace FullTextIndexer.Core.Indexes
{
    /// <summary>
    /// The methods in this class can serialise and deserialise an IndexData instance in much less space than using the .Net BinaryFormatter (approx 1/12 the space in preliminary testing)
    /// </summary>
    public static class IndexDataSerialiser<TKey>
	{
		/// <summary>
		/// This will throw an ArgumentNullException for null source or stream references or an IndexDataSerialisationException if the data read from the stream is invalid
		/// </summary>
		public static void Serialise(IndexData<TKey> source, Stream stream)
		{
			if (source == null)
				throw new ArgumentNullException("source");
			if (stream == null)
				throw new ArgumentNullException("stream");

			byte[] matchData;
			ImmutableList<TKey> allKeys;
			using (var matchDataMemoryStream = new MemoryStream())
			{
				using (var matchDataWriter = new BinaryWriter(matchDataMemoryStream))
				{
					allKeys = WriteMatchDataAndReturnReferencedKeys(source, matchDataWriter);
					matchData = matchDataMemoryStream.ToArray();
				}
			}

			using (var writer = new BinaryWriter(stream))
			{
				writer.Write("INDEXDATA\n");

				writer.Write("KEYCOMPARER\n");
				SerialiseItem(source.KeyComparer, writer);

				writer.Write("STRINGNORMALISER\n");
				SerialiseItem(source.TokenComparer, writer);

				writer.Write("KEYS\n");
				SerialiseItem(allKeys, writer);

				writer.Write("MATCHES\n");
				writer.Write(matchData);
			}
		}

		/// <summary>
		/// This will throw an ArgumentNullException for null source or stream references or an IndexDataSerialisationException if the data read from the stream is invalid.
		/// It will never return null.
		/// </summary>
		public static IndexData<TKey> Deserialise(Stream stream)
		{
			if (stream == null)
				throw new ArgumentNullException("stream");

			using (var reader = new BinaryReader(stream))
			{
				if (reader.ReadString() != "INDEXDATA\n")
					throw new IndexDataSerialisationException("Error validating header");

				if (reader.ReadString() != "KEYCOMPARER\n")
					throw new IndexDataSerialisationException("Error deserialising keyComparer");
				IEqualityComparer<TKey> keyComparer;
				try
				{
					keyComparer = DeserialiseItem<IEqualityComparer<TKey>>(reader);
				}
				catch (Exception e)
				{
					throw new IndexDataSerialisationException("Error deserialising keyComparer", e);
				}

				if (reader.ReadString() != "STRINGNORMALISER\n")
					throw new IndexDataSerialisationException("Error deserialising stringNormaliser");
				IStringNormaliser stringNormaliser;
				try
				{
					stringNormaliser = DeserialiseItem<IStringNormaliser>(reader);
				}
				catch (Exception e)
				{
					throw new IndexDataSerialisationException("Error deserialising stringNormaliser", e);
				}

				if (reader.ReadString() != "KEYS\n")
					throw new IndexDataSerialisationException("Error deserialising keys");
				ImmutableList<TKey> keys;
				try
				{
					keys = DeserialiseItem<ImmutableList<TKey>>(reader);
				}
				catch (Exception e)
				{
					throw new IndexDataSerialisationException("Error deserialising keys", e);
				}

				if (reader.ReadString() != "MATCHES\n")
					throw new IndexDataSerialisationException("Error deserialising token matches");
				try
				{
					return RebuildIndexFromMatchDataAndReferencedKeys(reader, keys, stringNormaliser, keyComparer);
				}
				catch (Exception e)
				{
					throw new IndexDataSerialisationException("Error deserialising token matches", e);
				}
			}
		}

		private static IndexData<TKey> RebuildIndexFromMatchDataAndReferencedKeys(
			BinaryReader reader,
			ImmutableList<TKey> keys,
			IStringNormaliser stringNormaliser,
			IEqualityComparer<TKey> keyComparer)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");
			if (keys == null)
				throw new ArgumentNullException("keys");
			if (stringNormaliser == null)
				throw new ArgumentNullException("stringNormaliser");
			if (keyComparer == null)
				throw new ArgumentNullException("keyComparer");

			var numberOfTokens = reader.ReadInt32();
			var matchDictionary = new Dictionary<string, NonNullImmutableList<WeightedEntry<TKey>>>(stringNormaliser);
			for (var tokenIndex = 0; tokenIndex < numberOfTokens; tokenIndex++)
			{
				var token = reader.ReadString();
				var numberOfMatchesForToken = reader.ReadInt32();
				var matches = NonNullImmutableList<WeightedEntry<TKey>>.Empty;
				for (var matchIndex = 0; matchIndex < numberOfMatchesForToken; matchIndex++)
				{
					var keyIndex = reader.ReadInt32();
					if ((keyIndex < 0) || (keyIndex >= keys.Count))
						throw new Exception("Invalid keyIndex (" + keyIndex + ")");

					var matchWeight = reader.ReadSingle();

					var numberOfSourceLocations = reader.ReadInt32();
					NonNullImmutableList<SourceFieldLocation> sourceLocationsIfRecorded;
					if (numberOfSourceLocations == 0)
						sourceLocationsIfRecorded = null;
					else
					{
						sourceLocationsIfRecorded = NonNullImmutableList<SourceFieldLocation>.Empty;
						for (var sourceLocationIndex = 0; sourceLocationIndex < numberOfSourceLocations; sourceLocationIndex++)
						{
							sourceLocationsIfRecorded = sourceLocationsIfRecorded.Add(
								new SourceFieldLocation(
									reader.ReadInt32(),
									reader.ReadInt32(),
									reader.ReadInt32(),
									reader.ReadInt32(),
									reader.ReadSingle()
								)
							);
						}
					}

					matches = matches.Add(
						new WeightedEntry<TKey>(
							keys[keyIndex],
							matchWeight,
							sourceLocationsIfRecorded
						)
					);
				}
				matchDictionary.Add(token, matches);
			}

			return new IndexData<TKey>(
				new TernarySearchTreeDictionary<NonNullImmutableList<WeightedEntry<TKey>>>(
					matchDictionary,
					stringNormaliser
				),
				keyComparer
			);
		}

		private static ImmutableList<TKey> WriteMatchDataAndReturnReferencedKeys(IndexData<TKey> data, BinaryWriter writer)
		{
			if (data == null)
				throw new ArgumentNullException("data");
			if (writer == null)
				throw new ArgumentNullException("writer");

			var allKeys = new List<TKey>();
			var allTokens = data.GetAllTokens();
			writer.Write(allTokens.Count);
			foreach (var token in allTokens)
			{
				var matchesForToken = data.GetMatches(token);
				writer.Write(token);
				writer.Write(matchesForToken.Count);
				foreach (var match in matchesForToken)
				{
					var keyIndexData = allKeys
						.Select((key, index) => new { Key = key, Index = index })
						.FirstOrDefault(k => data.KeyComparer.Equals(k.Key, match.Key));
					int keyIndex;
					if (keyIndexData == null)
					{
						allKeys.Add(match.Key);
						keyIndex = allKeys.Count - 1;
					}
					else
						keyIndex = keyIndexData.Index;

					writer.Write(keyIndex);
					writer.Write(match.Weight);
					if (match.SourceLocationsIfRecorded == null)
						writer.Write(0);
					else
					{
						writer.Write(match.SourceLocationsIfRecorded.Count);
						foreach (var sourceLocation in match.SourceLocationsIfRecorded)
						{
							writer.Write(sourceLocation.SourceFieldIndex);
							writer.Write(sourceLocation.TokenIndex);
							writer.Write(sourceLocation.SourceIndex);
							writer.Write(sourceLocation.SourceTokenLength);
							writer.Write(sourceLocation.MatchWeightContribution);
						}
					}
				}
			}
			return allKeys.ToImmutableList();
		}

		private static void SerialiseItem(object data, BinaryWriter writer)
		{
			if (data == null)
				throw new ArgumentNullException("data");
			if (writer == null)
				throw new ArgumentNullException("writer");

			byte[] serialiseData;
			using (var memoryStream = new MemoryStream())
			{
				new BinaryFormatter().Serialize(memoryStream, data);
				serialiseData = memoryStream.ToArray();
			}
			writer.Write(serialiseData.Length);
			writer.Write(serialiseData);
		}

		private static T DeserialiseItem<T>(BinaryReader reader)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");

			var dataLength = reader.ReadInt32();
			using (var memoryStream = new MemoryStream(reader.ReadBytes(dataLength)))
			{
				return (T)new BinaryFormatter().Deserialize(memoryStream);
			}
		}

		[Serializable]
		public class IndexDataSerialisationException : Exception
		{
			public IndexDataSerialisationException() : base() { }
			public IndexDataSerialisationException(string message) : base(message) { }
			public IndexDataSerialisationException(string message, Exception innerException) : base(message, innerException) { }
			protected IndexDataSerialisationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
		}
	}
}