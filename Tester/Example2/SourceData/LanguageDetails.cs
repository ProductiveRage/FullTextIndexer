using System;

namespace Tester.Example2.SourceData
{
    [Serializable]
    public sealed class LanguageDetails
    {
        public LanguageDetails(int key, string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Null/blank code");

            Key = key;
            Code = code.Trim();
        }

        public int Key { get; private set; }

        /// <summary>
        /// This will never be null but it may be empty
        /// </summary>
        public string Code { get; private set; }

        public override bool Equals(object obj)
        {
            var objLanguage = obj as LanguageDetails;
            if (objLanguage == null)
                return false;
            return objLanguage.Key == Key;
        }
        public override int GetHashCode()
        {
            return String.Format("LanguageDetails-{0}", Key).GetHashCode();
        }
    }
}
