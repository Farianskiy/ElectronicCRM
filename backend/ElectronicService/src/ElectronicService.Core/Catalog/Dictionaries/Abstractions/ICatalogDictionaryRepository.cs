using ElectronicService.Domain.Catalog.Dictionaries;

namespace ElectronicService.Core.Catalog.Dictionaries.Abstractions;

public interface ICatalogDictionaryRepository
{
    Task<CatalogDictionaryTerm?> GetByIdAsync(Guid termId, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(CatalogDictionaryTerm term, CancellationToken cancellationToken = default);

    void Add(CatalogDictionaryTerm term);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}