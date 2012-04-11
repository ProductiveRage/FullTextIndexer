using System;

namespace Tester.Example1.SourceData
{
    [Serializable]
    public class Product
    {
        public Product(int key, string name, string keywords, AddressDetails address, string description)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Null/empty name");

            Key = key;
            Name = name.Trim();
            Keywords = (keywords ?? "").Trim();
            Address = address;
            Description = (description ?? "").Trim();
        }

        public int Key { get; private set; }

        /// <summary>
        /// This will never be null or empty
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// This will never be null but it may be empty
        /// </summary>
        public string Keywords { get; private set; }

        /// <summary>
        /// This will never be null but it may be empty
        /// </summary>
        public AddressDetails Address { get; private set; }

        /// <summary>
        /// This will never be null but it may be empty
        /// </summary>
        public string Description { get; private set; }
    }
}
