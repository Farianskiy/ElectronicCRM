using ElectronicService.Domain.Catalog.ImportBatches;

namespace ElectronicService.Core.Catalog
    .ImportBatches.Abstractions;

public interface ICatalogImportBatchRepository
{
    /*
     * Добавляется весь aggregate:
     *
     * CatalogImportBatch
     * +
     * CatalogImportFile
     *
     * Отдельно добавлять File в DbContext
     * не требуется.
     */
    void Add(CatalogImportBatch batch);

    /*
     * Один SaveChanges сохранит batch и файл
     * в одной транзакции.
     */
    Task SaveChangesAsync(
        CancellationToken cancellationToken =
            default);
}