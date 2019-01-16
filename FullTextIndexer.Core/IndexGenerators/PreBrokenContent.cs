using System;
using FullTextIndexer.Common.Lists;

namespace FullTextIndexer.Core.IndexGenerators
{
	/// <summary>
	/// This is content extracted from a source item for a particular key, before it is broken down into tokens
	/// </summary>
	public class PreBrokenContent<TKey>
    {
        public PreBrokenContent(TKey key, NonNullOrEmptyStringList content)
        {
            if (key == null)
                throw new ArgumentNullException("key");
			if (content == null)
				throw new ArgumentNullException("content");

            Key = key;
            Content = content;
        }
		public PreBrokenContent(TKey key, string contentIfAny) : this(key, GetContentSections(contentIfAny)) { }

		private static NonNullOrEmptyStringList GetContentSections(string contentIfAny)
		{
			if (string.IsNullOrWhiteSpace(contentIfAny))
				return NonNullOrEmptyStringList.Empty;
			return new NonNullOrEmptyStringList(contentIfAny);
		}

        /// <summary>
        /// This will never be null
        /// </summary>
        public TKey Key { get; private set; }

        /// <summary>
        /// This will never be null but it may be empty if there was no content for the Content Retriever to extract
        /// </summary>
        public NonNullOrEmptyStringList Content { get; private set; }
    }
}
