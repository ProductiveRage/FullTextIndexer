using System;
using System.Collections.Generic;
using System.Linq;
using Common.Lists;

namespace Tester.Example2.SourceData
{
    [Serializable]
    public class DescriptionDetails
    {
        public DescriptionDetails(LanguageDetails language, bool isDefault, IEnumerable<int> channels, string longDescription, string shortDescription)
        {
            if (language == null)
                throw new ArgumentNullException("language");
            if (isDefault && ((channels != null) && channels.Any()))
                throw new ArgumentException("If isDefault is set then channels must be null or empty");
            if (!isDefault && ((channels == null) || !channels.Any()))
                throw new ArgumentException("If isDefault is not set then channels must not be null or empty");
            if (string.IsNullOrWhiteSpace(longDescription) && string.IsNullOrWhiteSpace(shortDescription))
                throw new ArgumentException("At least one of longDescription and shortDescription must be non-null/empty");

            Language = language;
            IsDefault = isDefault;
            Channels = channels.Distinct().ToImmutableList();
            LongDescription = string.IsNullOrWhiteSpace(longDescription) ? "" : longDescription.Trim();
            ShortDescription = string.IsNullOrWhiteSpace(shortDescription) ? "" : shortDescription.Trim();
        }

        /// <summary>
        /// This will never be null
        /// </summary>
        public LanguageDetails Language { get; private set; }
        
        public bool IsDefault { get; private set; }
        
        /// <summary>
        /// This will never be null. It will be empty if and only if IsDefault is true.
        /// </summary>
        public ImmutableList<int> Channels { get; private set; }
        
        /// <summary>
        /// This will never be null but it may be blank. At least one of LongDescription and ShortDescription will be non-blank.
        /// </summary>
        public string LongDescription { get; private set; }

        /// <summary>
        /// This will never be null but it may be blank. At least one of LongDescription and ShortDescription will be non-blank.
        /// </summary>
        public string ShortDescription { get; private set; }
    }
}
