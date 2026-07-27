using ElectronicService.Core.Catalog.ImportBatches.Abstractions;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

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

        _dbContext.CatalogImportBatches.Add(
            batch);
    }

    public Task<CatalogImportBatch?>
    GetByIdAsync(
        Guid batchId,
        CancellationToken cancellationToken =
            default)
    {
        return _dbContext.CatalogImportBatches
            .FirstOrDefaultAsync(
                batch => batch.Id == batchId,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<
        CatalogImportColumn>>
        GetColumnsForAnalysisAsync(
            Guid batchId,
            CancellationToken cancellationToken =
                default)
    {
        return await _dbContext
            .CatalogImportColumns
            .AsNoTracking()
            .Where(column =>
                column.BatchId == batchId)
            .OrderBy(column =>
                column.SourceColumnNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyCollection<
    CatalogImportColumn>>
    GetColumnsForUpdateAsync(
        Guid batchId,
        CancellationToken cancellationToken =
            default)
    {
        return await _dbContext
            .CatalogImportColumns
            .Where(column =>
                column.BatchId == batchId)
            .OrderBy(column =>
                column.SourceColumnNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<CatalogImportBatch?>
        GetByIdWithFileAsync(
            Guid batchId,
            CancellationToken cancellationToken =
                default)
    {
        return _dbContext.CatalogImportBatches
            .Include(batch => batch.File)
            .FirstOrDefaultAsync(
                batch => batch.Id == batchId,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<CatalogImportRow>>
        GetRowsAsync(
            Guid batchId,
            CatalogImportRowStatus? status,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
    {
        var query = _dbContext.CatalogImportRows
            .AsNoTracking()
            .Where(row => row.BatchId == batchId);

        if (status.HasValue)
        {
            query = query.Where(
                row => row.Status == status.Value);
        }

        return await query
            .OrderBy(row => row.RowNumber)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<CatalogImportRow?>
        GetRowByIdAsync(
            Guid batchId,
            Guid rowId,
            CancellationToken cancellationToken =
                default)
    {
        return _dbContext.CatalogImportRows
            .FirstOrDefaultAsync(
                row =>
                    row.BatchId == batchId
                    && row.Id == rowId,
                cancellationToken);
    }

    public Task<int> CountRowsAsync(
        Guid batchId,
        CatalogImportRowStatus? status,
        CancellationToken cancellationToken =
            default)
    {
        var query = _dbContext.CatalogImportRows
            .AsNoTracking()
            .Where(row =>
                row.BatchId == batchId);

        if (status.HasValue)
        {
            query = query.Where(row =>
                row.Status == status.Value);
        }

        return query.CountAsync(cancellationToken);
    }

    public async Task ReplaceAnalysisAsync(
        CatalogImportBatch batch,
        IReadOnlyCollection<CatalogImportColumn>
            columns,
        IReadOnlyCollection<CatalogImportRow>
            rows,
        CancellationToken cancellationToken =
            default)
    {
        ArgumentNullException.ThrowIfNull(batch);
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(rows);

        /*
         * ExecuteDeleteAsync выполняется сразу,
         * а не при SaveChanges.
         *
         * Поэтому все действия обязательно
         * объединяем одной транзакцией.
         */
        await using var transaction =
            await _dbContext.Database
                .BeginTransactionAsync(
                    cancellationToken)
                .ConfigureAwait(false);

        /*
         * Сначала удаляем строки.
         * Затем колонки.
         *
         * Старый анализ полностью заменяется
         * новым результатом.
         */
        await _dbContext.CatalogImportRows
            .Where(row =>
                row.BatchId == batch.Id)
            .ExecuteDeleteAsync(
                cancellationToken)
            .ConfigureAwait(false);

        await _dbContext.CatalogImportColumns
            .Where(column =>
                column.BatchId == batch.Id)
            .ExecuteDeleteAsync(
                cancellationToken)
            .ConfigureAwait(false);

        _dbContext.CatalogImportColumns
            .AddRange(columns);

        _dbContext.CatalogImportRows
            .AddRange(rows);

        /*
         * Здесь также сохраняется новый статус
         * отслеживаемого CatalogImportBatch.
         */
        await _dbContext
            .SaveChangesAsync(
                cancellationToken)
            .ConfigureAwait(false);

        await transaction
            .CommitAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken =
            default)
    {
        return _dbContext.SaveChangesAsync(
            cancellationToken);
    }
}