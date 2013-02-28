using System;

namespace FullTextIndexer.Core.IndexGenerators
{
    /// <summary>
    /// This is content extract from a source item for a particular key, before it is broken down into tokens
    /// </summary>
    public class PreBrokenContent<TKey>
    {
        public PreBrokenContent(TKey key, string content)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Null/blank content specified");

            Key = key;
            Content = content;
        }

        /// <summary>
        /// This will never be null
        /// </summary>
        public TKey Key { get; private set; }

        /// <summary>
        /// This will never be null or empty
        /// </summary>
        public string Content { get; private set; }
    }
}
