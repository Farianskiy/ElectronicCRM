using System.Text.Json;
using ElectronicService.Core.Catalog
    .ImportBatches.Preview;
using ElectronicService.Domain.Catalog
    .ImportBatches;
using ElectronicService.Infrastructure.Postgres
    .Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres
    .Catalog.Queries;

public sealed class CatalogImportPreviewReader
    : ICatalogImportPreviewReader
{
    private readonly ElectronicDbContext
        _dbContext;

    public CatalogImportPreviewReader(
        ElectronicDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<CatalogImportBatchDetailsResult?>
        GetBatchAsync(
            Guid batchId,
            CancellationToken cancellationToken =
                default)
    {
        return _dbContext.CatalogImportBatches
            .AsNoTracking()
            .Where(batch =>
                batch.Id == batchId)
            .Select(batch =>
                new CatalogImportBatchDetailsResult(
                    batch.Id,
                    batch.CreatedByUserId,
                    batch.ProductTypeId,
                    batch.OriginalFileName,
                    batch.ContentType,
                    batch.FileSizeBytes,
                    batch.FileSha256,
                    batch.Status,
                    batch.RowsCount,
                    batch.ValidRowsCount,
                    batch.ErrorRowsCount,
                    batch.CreatedAtUtc,
                    batch.UpdatedAtUtc,
                    batch.SubmittedAtUtc,
                    batch.ReviewedAtUtc,
                    batch.AppliedAtUtc,
                    batch.RejectedAtUtc,
                    batch.RejectionReason,
                    batch.FailureReason,
                    batch.Version))
            .FirstOrDefaultAsync(
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<
        CatalogImportColumnResult>>
        GetColumnsAsync(
            Guid batchId,
            CancellationToken cancellationToken =
                default)
    {
        var columns =
            await _dbContext
                .CatalogImportColumns
                .AsNoTracking()
                .Where(column =>
                    column.BatchId == batchId)
                .OrderBy(column =>
                    column.SourceColumnNumber)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        var definitionIds =
            columns
                .Where(column =>
                    column
                        .CharacteristicDefinitionId
                        .HasValue)
                .Select(column =>
                    column
                        .CharacteristicDefinitionId!
                        .Value)
                .Distinct()
                .ToArray();

        var definitions =
            await _dbContext
                .CharacteristicDefinitions
                .AsNoTracking()
                .Where(definition =>
                    definitionIds.Contains(
                        definition.Id))
                .ToDictionaryAsync(
                    definition => definition.Id,
                    cancellationToken)
                .ConfigureAwait(false);

        return columns
            .Select(column =>
            {
                definitions.TryGetValue(
                    column
                        .CharacteristicDefinitionId
                    ?? Guid.Empty,
                    out var definition);

                return new CatalogImportColumnResult(
                    column.Id,
                    column.SourceColumnNumber,
                    column.SourceHeader,
                    column.NormalizedSourceHeader,
                    column.TargetKind,
                    column
                        .CharacteristicDefinitionId,
                    definition?.Code,
                    definition?.Name,
                    definition?.DataType.ToString(),
                    definition?.Unit,
                    column.Confidence,
                    column.IsConfirmed,
                    column.IsMapped);
            })
            .ToList();
    }

    public async Task<CatalogImportRowsPageResult>
        GetRowsAsync(
            Guid batchId,
            int pageNumber,
            int pageSize,
            CatalogImportRowStatus? status,
            CancellationToken cancellationToken =
                default)
    {
        var query =
            _dbContext.CatalogImportRows
                .AsNoTracking()
                .Where(row =>
                    row.BatchId == batchId);

        if (status.HasValue)
        {
            query = query.Where(row =>
                row.Status == status.Value);
        }

        var totalCount =
            await query
                .CountAsync(cancellationToken)
                .ConfigureAwait(false);

        var skip =
            (pageNumber - 1)
            * pageSize;

        var rows =
            await query
                .OrderBy(row =>
                    row.RowNumber)
                .Skip(skip)
                .Take(pageSize)
                .Select(row =>
                    new RawRowResult(
                        row.Id,
                        row.RowNumber,
                        row.Status,
                        row.RawDataJson,
                        row.NormalizedDataJson,
                        row.IssuesJson,
                        row.WarningsJson))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        var items =
            rows
                .Select(row =>
                    new CatalogImportRowResult(
                        row.RowId,
                        row.RowNumber,
                        row.Status,
                        ParseJson(
                            row.RawDataJson,
                            "{}"),
                        ParseJson(
                            row.NormalizedDataJson,
                            "{}"),
                        ParseJson(
                            row.IssuesJson,
                            "[]"),
                        ParseJson(
                            row.WarningsJson,
                            "[]")))
                .ToList();

        var totalPages = 0;

        if (totalCount > 0)
        {
            totalPages =
                (totalCount + pageSize - 1)
                / pageSize;
        }

        return new CatalogImportRowsPageResult(
            pageNumber,
            pageSize,
            totalCount,
            totalPages,
            items);
    }

    private static JsonElement ParseJson(
        string value,
        string fallback)
    {
        try
        {
            using var document =
                JsonDocument.Parse(value);

            return document
                .RootElement
                .Clone();
        }
        catch (JsonException)
        {
            using var document =
                JsonDocument.Parse(fallback);

            return document
                .RootElement
                .Clone();
        }
    }

    private sealed record RawRowResult(
        Guid RowId,
        int RowNumber,
        CatalogImportRowStatus Status,
        string RawDataJson,
        string NormalizedDataJson,
        string IssuesJson,
        string WarningsJson);
}