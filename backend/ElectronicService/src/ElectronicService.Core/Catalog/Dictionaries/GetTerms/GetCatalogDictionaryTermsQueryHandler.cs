using ElectronicService.Core.Catalog.Dictionaries.Abstractions;

namespace ElectronicService.Core.Catalog.Dictionaries.GetTerms;

public sealed class GetCatalogDictionaryTermsQueryHandler
{
    private readonly ICatalogDictionaryReader _dictionaryReader;

    public GetCatalogDictionaryTermsQueryHandler(
        ICatalogDictionaryReader dictionaryReader)
    {
        _dictionaryReader = dictionaryReader;
    }

    public Task<IReadOnlyCollection<CatalogDictionaryTermResult>> Handle(
        GetCatalogDictionaryTermsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return _dictionaryReader.GetTermsAsync(cancellationToken);
    }
}