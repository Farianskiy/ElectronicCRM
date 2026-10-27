using ElectronicService.Domain.Catalog.Products;

namespace ElectronicService.Core.Catalog.Products.Analogies;

public interface ICatalogProductAnalogyConfigurationRepository
{
    Task<CatalogProductAnalogyConfiguration?> GetActiveAsync(
        Guid manufacturerId,
        Guid productTypeId,
        CancellationToken cancellationToken = default);

    Task<CatalogProductAnalogyConfiguration?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CatalogProductAnalogyConfiguration>> ListAsync(
        Guid manufacturerId,
        Guid productTypeId,
        CancellationToken cancellationToken = default);

    Task<int> GetNextVersionNumberAsync(
        Guid manufacturerId,
        Guid productTypeId,
        CancellationToken cancellationToken = default);

    void Add(CatalogProductAnalogyConfiguration configuration);

    void Add(CatalogProductAnalogyEvaluationReport report);

    Task<IReadOnlyCollection<CatalogProductAnalogyEvaluationReport>> ListReportsAsync(
        Guid configurationId,
        CancellationToken cancellationToken = default);

    Task<CatalogProductAnalogyEvaluationReport?> GetReportAsync(
        Guid reportId,
        CancellationToken cancellationToken = default);

    Task<bool> TryActivateAsync(
        CatalogProductAnalogyConfiguration configuration,
        uint expectedVersion,
        CancellationToken cancellationToken = default);

    Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken = default);
}
