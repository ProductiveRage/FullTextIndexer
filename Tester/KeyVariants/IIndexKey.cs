using System;
using Tester.SourceData;

namespace Tester.KeyVariants
{
    public interface IIndexKey : IEquatable<IIndexKey>
    {
        int ProductKey { get; }
        
        /// <summary>
        /// This will throw an exception for a null language reference
        /// </summary>
        bool IsApplicableFor(LanguageDetails language, int channelKey);
        
        new bool Equals(IIndexKey obj);
    }
}
