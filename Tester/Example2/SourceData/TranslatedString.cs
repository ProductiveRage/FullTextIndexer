using System;
using System.Collections.Generic;

namespace Tester.Example2.SourceData
{
    [Serializable]
    public class TranslatedString
    {
        private Dictionary<LanguageDetails, string> _data;
        public TranslatedString(string defaultValue, IDictionary<LanguageDetails, string> data)
        {
            if (string.IsNullOrWhiteSpace(defaultValue))
                throw new ArgumentException("Null/blank defaultValue specified");
            if (data == null)
                throw new ArgumentNullException("data");

            var dataTidied = new Dictionary<LanguageDetails, string>();
            foreach (var key in data.Keys)
            {
                var value = data[key];
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Null/blank value encountered in data");
                dataTidied.Add(key, value.Trim());
            }

            _data = dataTidied;
            DefaultValue = defaultValue;
        }

        /// <summary>
        /// This will never be null or empty
        /// </summary>
        public string DefaultValue { get; private set; }

        /// <summary>
        /// This will return null if there is no content for the specified language, it will never return an empty string
        /// </summary>
        public string GetTranslation(LanguageDetails language)
        {
            if (language == null)
                throw new ArgumentNullException("language");

            if (_data.ContainsKey(language))
                return _data[language];
            
            return DefaultValue;
        }
    }
}
