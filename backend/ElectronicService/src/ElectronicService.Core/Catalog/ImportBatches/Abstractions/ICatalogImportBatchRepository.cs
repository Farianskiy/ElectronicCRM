using ElectronicService.Domain.Catalog.ImportBatches;

namespace ElectronicService.Core.Catalog.ImportBatches.Abstractions;

public interface ICatalogImportBatchRepository
{
    void Add(
        CatalogImportBatch batch);

    Task<CatalogImportBatch?>
        GetByIdWithFileAsync(
            Guid batchId,
            CancellationToken cancellationToken =
                default);

    Task ReplaceAnalysisAsync(
        CatalogImportBatch batch,
        IReadOnlyCollection<CatalogImportColumn>
            columns,
        IReadOnlyCollection<CatalogImportRow>
            rows,
        CancellationToken cancellationToken =
            default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken =
            default);
}