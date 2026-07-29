using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.ImportBatches.Cleanup;

public sealed class CatalogImportCleanupService
{
    private static readonly CatalogImportBatchStatus[] ExpirableStatuses =
    [
        CatalogImportBatchStatus.Uploaded,
        CatalogImportBatchStatus.MappingRequired,
        CatalogImportBatchStatus.NeedsCorrection,
        CatalogImportBatchStatus.Ready
    ];

    private readonly ElectronicDbContext _dbContext;

    public CatalogImportCleanupService(ElectronicDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> DeleteExpiredAsync(DateTime cutoffUtc, int batchSize, CancellationToken cancellationToken = default)
    {
        if (cutoffUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Cleanup cutoff must use UTC.",
                nameof(cutoffUtc));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(batchSize, 1);

        var totalDeletedCount = 0;

        while (true)
        {
            var batchIds = await CreateExpiredQuery(cutoffUtc)
                .OrderBy(batch => batch.UpdatedAtUtc ?? batch.CreatedAtUtc)
                .Select(batch => batch.Id)
                .Take(batchSize)
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);

            if (batchIds.Length == 0)
            {
                return totalDeletedCount;
            }

            /*
             * Повторно проверяем статус и дату непосредственно
             * в DELETE-запросе. Если пакет успели отредактировать
             * или отправить на проверку, он не будет удалён.
             */
            var deletedCount = await CreateExpiredQuery(cutoffUtc)
                .Where(batch => batchIds.Contains(batch.Id))
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            totalDeletedCount += deletedCount;

            /*
             * deletedCount может быть равен нулю при конкурентном
             * изменении всех выбранных пакетов.
             */
            if (deletedCount == 0 || batchIds.Length < batchSize)
            {
                return totalDeletedCount;
            }
        }
    }

    private IQueryable<CatalogImportBatch> CreateExpiredQuery(DateTime cutoffUtc)
    {
        return _dbContext.CatalogImportBatches
            .Where(batch =>
                ExpirableStatuses.Contains(batch.Status)
                && (batch.UpdatedAtUtc ?? batch.CreatedAtUtc) < cutoffUtc);
    }
}