using System;
using System.Collections.Generic;
using System.Linq;
using Common.Lists;
using Common.Logging;
using FullTextIndexer;
using FullTextIndexer.Indexes;
using FullTextIndexer.IndexGenerators;
using FullTextIndexer.TokenBreaking;
using Tester.KeyVariants;
using Tester.SourceData;

namespace Tester
{
    public class ProductIndexGenerator
    {
        private NonNullImmutableList<LanguageDetails> _activeLanguages;
        private LanguageDetails _defaultLanguage;
        private ITokenBreaker _tokenBreaker;
        private IEqualityComparer<string> _sourceStringComparer;
        private ILogger _logger;
        public ProductIndexGenerator(
            NonNullImmutableList<LanguageDetails> activeLanguages,
            LanguageDetails defaultLanguage,
            ITokenBreaker tokenBreaker,
            IEqualityComparer<string> sourceStringComparer,
            ILogger logger)
        {
            if (activeLanguages == null)
                throw new ArgumentNullException("activeLanguages");
            if (defaultLanguage == null)
                throw new ArgumentNullException("defaultLanguage");
            if (!activeLanguages.Contains(defaultLanguage))
                throw new ArgumentException("defaultLanguage must be one of the listed activeLanguages values");
            if (tokenBreaker == null)
                throw new ArgumentNullException("tokenBreaker");
            if (sourceStringComparer == null)
                throw new ArgumentNullException("sourceStringComparer");
            if (logger == null)
                throw new ArgumentNullException("logger");

            _activeLanguages = activeLanguages;
            _defaultLanguage = defaultLanguage;
            _tokenBreaker = tokenBreaker;
            _sourceStringComparer = sourceStringComparer;
            _logger = logger;
        }

        public IndexData<IIndexKey> Generate(NonNullImmutableList<Product> data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            var activeChannelKeys = data.SelectMany(p => p.Channels).Distinct().ToArray();
            var contentRetrievers = new List<IndexGenerator<Product, IIndexKey>.ContentRetriever>();
            foreach (var language in _activeLanguages)
            {
                // Take a copy of languageKey to ensure the correct value is used in the following lambdas
                var languageForEntry = language;
                var stopwords = Constants.GetStopWords(languageForEntry.Code);

                // Instantiate content retrievers for the Product Name and Keywords in the current language
                contentRetrievers.Add(
                    new IndexGenerator<Product, IIndexKey>.ContentRetriever(
                        p => new IndexGenerator<Product, IIndexKey>.PreBrokenContent(
                            new LanguageScopedIndexKey(p.Key, languageForEntry),
                            p.Name.GetTranslation(languageForEntry)
                        ),
                        token => 15f * (stopwords.Contains(token, _sourceStringComparer) ? 0.01f : 1f)
                    )
                );
                contentRetrievers.Add(
                    new IndexGenerator<Product, IIndexKey>.ContentRetriever(
                        p =>
                        {
                            if (p.Keywords == null)
                                return null;
                            return new IndexGenerator<Product, IIndexKey>.PreBrokenContent(
                                new LanguageScopedIndexKey(p.Key, languageForEntry),
                                p.Keywords.GetTranslation(languageForEntry)
                            );
                        },
                        token => 3f * (stopwords.Contains(token, _sourceStringComparer) ? 0.01f : 1f)
                    )
                );

                // Instantiate content retrievers for the Product Description in the current language, dealing with Description content for each active channel
                foreach (var channelKey in activeChannelKeys)
                {
                    // Take a copy of channelKey to ensure the correct value is used in the following lambdas
                    var channelKeyForEntry = channelKey;
                    contentRetrievers.Add(
                        new IndexGenerator<Product, IIndexKey>.ContentRetriever(
                            p =>
                            {
                                var description = GetDescription(p, languageForEntry, channelKeyForEntry);
                                if ((description == null) || (description.LongDescription == ""))
                                    return null;
                                return new IndexGenerator<Product, IIndexKey>.PreBrokenContent(
                                    new LanguageScopedIndexKey(p.Key, languageForEntry),
                                    description.LongDescription
                                );
                            },
                            token => stopwords.Contains(token, _sourceStringComparer) ? 0.01f : 1f
                        )
                    );
                    contentRetrievers.Add(
                        new IndexGenerator<Product, IIndexKey>.ContentRetriever(
                            p =>
                            {
                                var description = GetDescription(p, languageForEntry, channelKeyForEntry);
                                if ((description == null) || (description.ShortDescription == ""))
                                    return null;
                                return new IndexGenerator<Product, IIndexKey>.PreBrokenContent(
                                    new LanguageScopedIndexKey(p.Key, languageForEntry),
                                    description.ShortDescription
                                );
                            },
                            token => stopwords.Contains(token, _sourceStringComparer) ? 0.01f : 1f
                        )
                    );
                }
                
                // Instantiate content retrieves for Address content in the current language - if the Address isn't in the current language then its content will be
                // skipped (Addresses aren't translated)
                contentRetrievers.Add(GetNonScopedContentRetriever(p => p.Address == null || !p.Address.Language.Equals(languageForEntry) ? null : p.Address.Address1, stopwords));
                contentRetrievers.Add(GetNonScopedContentRetriever(p => p.Address == null || !p.Address.Language.Equals(languageForEntry) ? null : p.Address.Address2, stopwords));
                contentRetrievers.Add(GetNonScopedContentRetriever(p => p.Address == null || !p.Address.Language.Equals(languageForEntry) ? null : p.Address.Address3, stopwords));
                contentRetrievers.Add(GetNonScopedContentRetriever(p => p.Address == null || !p.Address.Language.Equals(languageForEntry) ? null : p.Address.Address4, stopwords));
                contentRetrievers.Add(GetNonScopedContentRetriever(p => p.Address == null || !p.Address.Language.Equals(languageForEntry) ? null : p.Address.Address5, stopwords));
                contentRetrievers.Add(GetNonScopedContentRetriever(p => p.Address == null || !p.Address.Language.Equals(languageForEntry) ? null : p.Address.Country, stopwords));
            }
            
            return new IndexGenerator<Product, IIndexKey>(
                contentRetrievers.ToNonNullImmutableList(),
                new IndexKeyEqualityComparer(),
                _sourceStringComparer,
                _tokenBreaker,
                weightedValues => weightedValues.Sum(),
                _logger
            ).Generate(data.ToNonNullImmutableList());
        }

        private IndexGenerator<Product, IIndexKey>.ContentRetriever GetNonScopedContentRetriever(Func<Product, string> valueRetriever, NonNullOrEmptyStringList stopWords)
        {
            if (valueRetriever == null)
                throw new ArgumentNullException("valueRetriever");
            if (stopWords == null)
                throw new ArgumentNullException("stopWords");

            return new IndexGenerator<Product, IIndexKey>.ContentRetriever(
                p =>
                {
                    var value = valueRetriever(p);
                    if (string.IsNullOrWhiteSpace(value))
                        return null;
                    return new IndexGenerator<Product, IIndexKey>.PreBrokenContent(
                        new NonScopedIndexKey(p.Key),
                        value
                    );
                },
                token => (stopWords.Contains(token, _sourceStringComparer) ? 0.01f : 1f)
            );
        }

        /// <summary>
        /// Try to get a Product's Description for a particular Language / Channel combination - if there is no match then fallback to one that matches Language and is
        /// marked as default. Return null if this finds no match either.
        /// </summary>
        private DescriptionDetails GetDescription(Product product, LanguageDetails language, int channelKey)
        {
            if (product == null)
                throw new ArgumentNullException("product");
            if (language == null)
                throw new ArgumentNullException("language");

            return
                product.Descriptions.FirstOrDefault(d => d.Language.Equals(language) && d.Channels.Contains(channelKey)) ??
                product.Descriptions.FirstOrDefault(d => d.Language.Equals(language) && d.IsDefault);
        }
    }
}
