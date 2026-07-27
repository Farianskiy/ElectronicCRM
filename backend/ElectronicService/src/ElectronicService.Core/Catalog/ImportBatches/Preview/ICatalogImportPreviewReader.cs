using ElectronicService.Domain.Catalog.ImportBatches;

namespace ElectronicService.Core.Catalog
    .ImportBatches.Preview;

public interface ICatalogImportPreviewReader
{
    Task<CatalogImportBatchDetailsResult?>
        GetBatchAsync(
            Guid batchId,
            CancellationToken cancellationToken =
                default);

    Task<IReadOnlyCollection<
        CatalogImportColumnResult>>
        GetColumnsAsync(
            Guid batchId,
            CancellationToken cancellationToken =
                default);

    Task<CatalogImportRowsPageResult>
        GetRowsAsync(
            Guid batchId,
            int pageNumber,
            int pageSize,
            CatalogImportRowStatus? status,
            CancellationToken cancellationToken =
                default);
}