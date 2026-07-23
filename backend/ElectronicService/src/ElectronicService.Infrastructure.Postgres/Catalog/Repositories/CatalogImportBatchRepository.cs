using ElectronicService.Core.Catalog.ImportBatches.Abstractions;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Infrastructure.Postgres.Data;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Repositories;

public sealed class CatalogImportBatchRepository
    : ICatalogImportBatchRepository
{
    private readonly ElectronicDbContext
        _dbContext;

    public CatalogImportBatchRepository(
        ElectronicDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(
        CatalogImportBatch batch)
    {
        ArgumentNullException.ThrowIfNull(batch);

        /*
         * EF Core увидит обязательную навигацию
         * batch.File и добавит обе сущности:
         *
         * INSERT catalog_import_batches
         * INSERT catalog_import_files
         */
        _dbContext.CatalogImportBatches.Add(
            batch);
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken =
            default)
    {
        return _dbContext.SaveChangesAsync(
            cancellationToken);
    }
}