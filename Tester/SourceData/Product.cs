using System;

namespace Tester.SourceData
{
    public class Product
    {
        public Product(int key, TranslatedString name, TranslatedString keywords, AddressDetails address)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            Key = key;
            Name = name;
            Keywords = keywords;
            Address = address;
        }

        public int Key { get; private set; }

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
    }
}
