using ElectronicService.Core.Catalog.Dictionaries.GetTerms;

namespace ElectronicService.Core.Catalog.Dictionaries.Abstractions;

public interface ICatalogDictionaryReader
{
    Task<IReadOnlyCollection<CatalogDictionaryTermResult>> GetTermsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CatalogDictionaryTermResult>> GetApprovedTermsAsync(
        CancellationToken cancellationToken = default);
}