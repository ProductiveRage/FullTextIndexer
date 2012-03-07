using System;

namespace Tester.SourceData
{
    public class AddressDetails
    {
        public AddressDetails(string address1, string address2, string address3, string address4)
        {
            if (string.IsNullOrWhiteSpace(address1))
                throw new ArgumentException("Null/blank address1");

            Address1 = address1.Trim();
            Address2 = (address2 ?? "").Trim();
            Address3 = (address3 ?? "").Trim();
            Address4 = (address4 ?? "").Trim();
        }

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
    }
}
