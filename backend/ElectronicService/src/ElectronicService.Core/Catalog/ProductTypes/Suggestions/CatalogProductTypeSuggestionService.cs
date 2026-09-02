using ElectronicService.Core.Catalog.Dictionaries.Abstractions;
using ElectronicService.Core.Catalog.Metadata.Abstractions;

namespace ElectronicService.Core.Catalog.ProductTypes.Suggestions;

public sealed class CatalogProductTypeSuggestionService
    : ICatalogProductTypeSuggestionService
{
    private readonly ICatalogDictionaryReader _dictionaryReader;
    private readonly ICatalogMetadataReader _metadataReader;

    public CatalogProductTypeSuggestionService(
        ICatalogDictionaryReader dictionaryReader,
        ICatalogMetadataReader metadataReader)
    {
        ArgumentNullException.ThrowIfNull(dictionaryReader);
        ArgumentNullException.ThrowIfNull(metadataReader);

        _dictionaryReader = dictionaryReader;
        _metadataReader = metadataReader;
    }

    public async Task<CatalogProductTypeSuggestionIndex> LoadIndexAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var productTypes = await _metadataReader
            .GetProductTypesAsync(cancellationToken)
            .ConfigureAwait(false);

        var dictionaryTerms = await _dictionaryReader
            .GetApprovedTermsAsync(cancellationToken)
            .ConfigureAwait(false);

        return new CatalogProductTypeSuggestionIndex(
            productTypes,
            dictionaryTerms);
    }

    public async Task<CatalogProductTypeSuggestionResult> SuggestAsync(
        string productName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productName);

        var index = await LoadIndexAsync(cancellationToken)
            .ConfigureAwait(false);

        return index.Suggest(
            productName,
            cancellationToken);
    }
}