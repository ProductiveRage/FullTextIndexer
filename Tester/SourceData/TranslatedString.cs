using System;
using System.Collections.Generic;

namespace Tester.SourceData
{
    [Serializable]
    public class TranslatedString
    {
        private Dictionary<int, string> _data;
        public TranslatedString(string defaultValue, IDictionary<int, string> data)
        {
            if (string.IsNullOrWhiteSpace(defaultValue))
                throw new ArgumentException("Null/blank defaultValue specified");
            if (data == null)
                throw new ArgumentNullException("data");

            var dataTidied = new Dictionary<int, string>();
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
        public string DefaultValue { get; private set; }
        public string GetTranslation(int languageKey)
        {
            if (_data.ContainsKey(languageKey))
                return _data[languageKey];
            return DefaultValue;
        }
    }
}
