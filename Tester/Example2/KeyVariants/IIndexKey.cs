using System;
using Tester.Example2.SourceData;

namespace Tester.Example2.KeyVariants
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
