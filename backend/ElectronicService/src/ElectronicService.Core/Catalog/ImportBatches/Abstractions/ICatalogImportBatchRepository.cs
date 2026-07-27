using ElectronicService.Domain.Catalog.ImportBatches;

namespace ElectronicService.Core.Catalog
    .ImportBatches.Abstractions;

public interface ICatalogImportBatchRepository
{
    void Add(
        CatalogImportBatch batch);
    /*
     * Используется анализатором,
     * которому требуется исходный Excel.
     */
    Task<CatalogImportBatch?>
        GetByIdWithFileAsync(
            Guid batchId,
            CancellationToken cancellationToken =
                default);

    /*
     * Неотслеживаемые колонки для
     * сохранения ручного mapping
     * при повторном анализе.
     */
    Task<IReadOnlyCollection<
        CatalogImportColumn>>
        GetColumnsForAnalysisAsync(
            Guid batchId,
            CancellationToken cancellationToken =
                default);

    /*
     * Отслеживаемые колонки для PUT mapping.
     */
    Task<List<CatalogImportColumn>>
        GetColumnsForUpdateAsync(
            Guid batchId,
            CancellationToken cancellationToken =
                default);

    Task<CatalogImportBatch?> GetByIdAsync(
    Guid batchId,
    CancellationToken cancellationToken = default);

    Task<CatalogImportRow?> GetRowByIdAsync(
        Guid batchId,
        Guid rowId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CatalogImportRow>>
        GetRowsAsync(
            Guid batchId,
            CatalogImportRowStatus? status,
            int skip,
            int take,
            CancellationToken cancellationToken = default);

    Task<int> CountRowsAsync(
        Guid batchId,
        CatalogImportRowStatus? status,
        CancellationToken cancellationToken = default);

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