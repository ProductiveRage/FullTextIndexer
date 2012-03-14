using System;
using System.Collections.Generic;
using System.Linq;
using Common.Lists;

namespace Tester.SourceData
{
    [Serializable]
    public class Product
    {
        public Product(int key, IEnumerable<int> channels, TranslatedString name, TranslatedString keywords, AddressDetails address, IEnumerable<DescriptionDetails> descriptions)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (channels == null)
                throw new ArgumentNullException("channels");
            if (!channels.Any())
                throw new ArgumentException("No channels specified");

            var descriptionsTidied = new List<DescriptionDetails>();
            if (descriptions != null)
            {
                foreach (var description in descriptions)
                {
                    if (description == null)
                        throw new ArgumentException("Null entry encountered in descriptions");
                    descriptionsTidied.Add(description);
                }
            }

            Key = key;
            Channels = channels.Distinct().ToImmutableList();
            Name = name;
            Keywords = keywords;
            Address = address;
            Descriptions = descriptionsTidied.ToNonNullImmutableList();
        }

        public int Key { get; private set; }

        /// <summary>
        /// This will never be null nor an empty list
        /// </summary>
        public ImmutableList<int> Channels { get; private set; }

        /// <summary>
        /// This will never be null or empty
        /// </summary>
        public TranslatedString Name { get; private set; }

        /// <summary>
        /// This may be null
        /// </summary>
        public TranslatedString Keywords { get; private set; }

        /// <summary>
        /// This may be null
        /// </summary>
        public AddressDetails Address { get; private set; }

        /// <summary>
        /// This will never be null but it may be an empty list
        /// </summary>
        public NonNullImmutableList<DescriptionDetails> Descriptions { get; private set; }
    }
}
