using System;

namespace Tester.SourceData
{
    [Serializable]
    public class AddressDetails
    {
        public AddressDetails(LanguageDetails language, string address1, string address2, string address3, string address4, string address5, string country)
        {
            if (language == null)
                throw new ArgumentNullException("language");
            if (string.IsNullOrWhiteSpace(address1))
                throw new ArgumentException("Null/blank address1");

            Language = language;
            Address1 = address1.Trim();
            Address2 = (address2 ?? "").Trim();
            Address3 = (address3 ?? "").Trim();
            Address4 = (address4 ?? "").Trim();
            Address5 = (address5 ?? "").Trim();
            Country = (country ?? "").Trim();
        }

        /// <summary>
        /// Addresses are never translated but it may be important to know what language an address is in. This will never be null.
        /// </summary>
        public LanguageDetails Language { get; private set; }

        /// <summary>
        /// This will never be null or empty
        /// </summary>
        public string Address1 { get; private set; }

        /// <summary>
        /// This will never be null but it may be empty
        /// </summary>
        public string Address2 { get; private set; }

        /// <summary>
        /// This will never be null but it may be empty
        /// </summary>
        public string Address3 { get; private set; }

        /// <summary>
        /// This will never be null but it may be empty
        /// </summary>
        public string Address4 { get; private set; }

        /// <summary>
        /// This will never be null but it may be empty
        /// </summary>
        public string Address5 { get; private set; }

        /// <summary>
        /// This will never be null but it may be empty
        /// </summary>
        public string Country { get; private set; }
    }
}
