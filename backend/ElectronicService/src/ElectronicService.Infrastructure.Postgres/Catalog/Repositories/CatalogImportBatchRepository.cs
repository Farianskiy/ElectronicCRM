using ElectronicService.Core.Catalog.ImportBatches.Abstractions;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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

    public Task<uint?> GetVersionAsync(
        Guid batchId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.CatalogImportBatches
            .AsNoTracking()
            .Where(batch => batch.Id == batchId)
            .Select(batch => (uint?)batch.Version)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public void Add(CatalogImportBatch batch)
    {
        ArgumentNullException.ThrowIfNull(batch);

        _dbContext.CatalogImportBatches.Add(batch);
    }

    public void Remove(CatalogImportBatch batch)
    {
        ArgumentNullException.ThrowIfNull(batch);

        _dbContext.CatalogImportBatches.Remove(batch);
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

    private IQueryable<CatalogImportRow> BuildRowsQuery(
    Guid batchId,
    CatalogImportRowStatus? status,
    string? search,
    string? issueCode,
    CatalogImportRowProblemKind? problemKind,
    string? manufacturerGroupKey)
    {
        IQueryable<CatalogImportRow> query;

        if (search is null && manufacturerGroupKey is null)
        {
            query = _dbContext.CatalogImportRows.AsNoTracking();
        }
        else
        {
            query = _dbContext.CatalogImportRows
                .FromSqlInterpolated(
                    $"""
                SELECT rows.*
                FROM catalog_import_rows AS rows
                WHERE
                (
                    CAST({search} AS text) IS NULL
                    OR strpos(
                        lower(
                            concat_ws(
                                ' ',
                                rows.normalized_data_json ->> 'name',
                                rows.normalized_data_json ->> 'article',
                                rows.normalized_data_json ->> 'manufacturer'
                            )
                        ),
                        lower(CAST({search} AS text))
                    ) > 0
                )
                AND
                (
                    CAST({manufacturerGroupKey} AS text) IS NULL
                    OR EXISTS
                    (
                        SELECT 1
                        FROM catalog_import_columns AS manufacturer_column
                        WHERE manufacturer_column.batch_id = rows.batch_id
                          AND manufacturer_column.target_kind = 'Manufacturer'
                          AND replace(
                                upper(
                                    btrim(
                                        rows.raw_data_json ->> manufacturer_column.source_column_number::text
                                    )
                                ),
                                'Ё',
                                'Е'
                              ) = CAST({manufacturerGroupKey} AS text)
                    )
                )
                """)
                .AsNoTracking();
        }

        query = query.Where(row => row.BatchId == batchId);

        if (status.HasValue)
        {
            query = query.Where(row => row.Status == status.Value);
        }

        if (issueCode is not null)
        {
            var issueFilterJson = JsonSerializer.Serialize(
                new[]
                {
                new
                {
                    code = issueCode
                }
                });

            query = query.Where(row =>
                EF.Functions.JsonContains(row.IssuesJson, issueFilterJson)
                || EF.Functions.JsonContains(row.WarningsJson, issueFilterJson));
        }

        if (problemKind == CatalogImportRowProblemKind.Error)
        {
            query = query.Where(row =>
                EF.Functions.JsonContains(row.IssuesJson, "[{}]"));
        }
        else if (problemKind == CatalogImportRowProblemKind.Warning)
        {
            query = query.Where(row =>
                EF.Functions.JsonContains(row.WarningsJson, "[{}]"));
        }

        return query;
    }

    public async Task<IReadOnlyCollection<CatalogImportRow>> GetRowsAsync(
        Guid batchId,
        CatalogImportRowStatus? status,
        string? search,
        string? issueCode,
        CatalogImportRowProblemKind? problemKind,
        string? manufacturerGroupKey,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = BuildRowsQuery(
            batchId,
            status,
            search,
            issueCode,
            problemKind,
            manufacturerGroupKey);

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

    public async Task<IReadOnlyCollection<CatalogImportRow>> GetRowsByIdsAsync(Guid batchId, IReadOnlyCollection<Guid> rowIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rowIds);

        var normalizedRowIds = rowIds
            .Where(rowId => rowId != Guid.Empty)
            .Distinct()
            .ToArray();

        if (normalizedRowIds.Length == 0)
        {
            return [];
        }

        return await _dbContext.CatalogImportRows
            .Where(row => row.BatchId == batchId && normalizedRowIds.Contains(row.Id))
            .OrderBy(row => row.RowNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<int> CountRowsAsync(
        Guid batchId,
        CatalogImportRowStatus? status,
        string? search,
        string? issueCode,
        CatalogImportRowProblemKind? problemKind,
        string? manufacturerGroupKey,
        CancellationToken cancellationToken = default)
    {
        return BuildRowsQuery(
            batchId,
            status,
            search,
            issueCode,
            problemKind,
            manufacturerGroupKey)
        .CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<CatalogImportBatch>> GetReviewQueueAsync(
    CatalogImportBatchStatus? status,
    int skip,
    int take,
    CancellationToken cancellationToken = default)
    {
        var query = _dbContext.CatalogImportBatches
            .AsNoTracking()
            .Where(batch =>
                batch.Status == CatalogImportBatchStatus.Submitted
                || batch.Status == CatalogImportBatchStatus.UnderReview);

        if (status.HasValue)
        {
            query = query.Where(batch => batch.Status == status.Value);
        }

        return await query
            .OrderBy(batch => batch.Status == CatalogImportBatchStatus.Submitted ? 0 : 1)
            .ThenBy(batch => batch.SubmittedAtUtc)
            .ThenBy(batch => batch.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<int> CountReviewQueueAsync(
    CatalogImportBatchStatus? status,
    CancellationToken cancellationToken = default)
    {
        var query = _dbContext.CatalogImportBatches
            .AsNoTracking()
            .Where(batch =>
                batch.Status == CatalogImportBatchStatus.Submitted
                || batch.Status == CatalogImportBatchStatus.UnderReview);

        if (status.HasValue)
        {
            query = query.Where(batch => batch.Status == status.Value);
        }

        return query.CountAsync(cancellationToken);
    }

    public async Task<bool> TrySaveChangesAsync(
    CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbContext
                .SaveChangesAsync(cancellationToken)
                .ConfigureAwait(false);

            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
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
         * До вызова ReplaceAnalysisAsync обработчик может:
         *
         * 1. пометить устаревшие Pending Feedback на удаление;
         * 2. изменить статус и статистику CatalogImportBatch.
         *
         * ExecuteDeleteAsync выполняется непосредственно в PostgreSQL
         * и не обрабатывает ожидающие изменения ChangeTracker.
         *
         * Поэтому сначала сохраняем отслеживаемые изменения внутри
         * уже открытой транзакции. Если дальнейшая замена анализа
         * завершится ошибкой, это сохранение также будет отменено.
         */
        await _dbContext
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        /*
         * Сначала удаляем строки.
         * Затем колонки.
         *
         * Старый анализ полностью заменяется
         * новым результатом.
         *
         * Pending Feedback уже удалены предыдущим SaveChangesAsync.
         * Finalized Feedback не удаляются. PostgreSQL обнулит их
         * ImportRowId через внешний ключ ON DELETE SET NULL,
         * сохранив ImportBatchId как источник происхождения.
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

    public async Task<IReadOnlyCollection<CatalogImportBatch>> GetByCreatorAsync(
        Guid createdByUserId,
        CatalogImportBatchStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.CatalogImportBatches
            .AsNoTracking()
            .Where(batch => batch.CreatedByUserId == createdByUserId);

        if (status.HasValue)
        {
            query = query.Where(batch => batch.Status == status.Value);
        }

        return await query
            .OrderByDescending(batch => batch.UpdatedAtUtc ?? batch.CreatedAtUtc)
            .ThenByDescending(batch => batch.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<int> CountByCreatorAsync(
        Guid createdByUserId,
        CatalogImportBatchStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.CatalogImportBatches
            .AsNoTracking()
            .Where(batch => batch.CreatedByUserId == createdByUserId);

        if (status.HasValue)
        {
            query = query.Where(batch => batch.Status == status.Value);
        }

        return query.CountAsync(cancellationToken);
    }

    public async Task<CatalogImportRowProblemsSummary> GetRowProblemCodesAsync(
        Guid batchId,
        CatalogImportRowStatus? status,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var baseQuery = BuildRowsQuery(
            batchId,
            status,
            search,
            issueCode: null,
            problemKind: null,
            manufacturerGroupKey: null);

        var totalRowsCount = await baseQuery
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        var errorRowsCount = await baseQuery
            .CountAsync(
                row => EF.Functions.JsonContains(row.IssuesJson, "[{}]"),
                cancellationToken)
            .ConfigureAwait(false);

        var warningRowsCount = await baseQuery
            .CountAsync(
                row => EF.Functions.JsonContains(row.WarningsJson, "[{}]"),
                cancellationToken)
            .ConfigureAwait(false);

        var statusValue = status?.ToString();

        var rows = await _dbContext.Database
            .SqlQuery<CatalogImportRowProblemCodeDbResult>(
                $"""
                SELECT
                    problem.code AS "Code",
                    COUNT(DISTINCT rows.id)::integer AS "RowsCount",
                    COUNT(DISTINCT rows.id)
                        FILTER (
                            WHERE problem.kind = 'error'
                        )::integer AS "ErrorRowsCount",
                    COUNT(DISTINCT rows.id)
                        FILTER (
                            WHERE problem.kind = 'warning'
                        )::integer AS "WarningRowsCount"
                FROM catalog_import_rows AS rows
                CROSS JOIN LATERAL
                (
                    SELECT DISTINCT
                        issue.value ->> 'code' AS code,
                        'error'::text AS kind
                    FROM jsonb_array_elements(
                        rows.issues_json
                    ) AS issue(value)

                    UNION ALL

                    SELECT DISTINCT
                        warning.value ->> 'code' AS code,
                        'warning'::text AS kind
                    FROM jsonb_array_elements(
                        rows.warnings_json
                    ) AS warning(value)
                ) AS problem
                WHERE rows.batch_id = {batchId}
                  AND
                  (
                      CAST({statusValue} AS text) IS NULL
                      OR rows.status = {statusValue}
                  )
                  AND
                  (
                      CAST({search} AS text) IS NULL
                      OR strpos(
                          lower(
                              concat_ws(
                                  ' ',
                                  rows.normalized_data_json ->> 'name',
                                  rows.normalized_data_json ->> 'article',
                                  rows.normalized_data_json ->> 'manufacturer'
                              )
                          ),
                          lower({search})
                      ) > 0
                  )
                  AND problem.code IS NOT NULL
                  AND btrim(problem.code) <> ''
                GROUP BY problem.code
                ORDER BY
                    COUNT(DISTINCT rows.id) DESC,
                    problem.code
                """)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = rows
            .Select(row =>
                new CatalogImportRowProblemCodeSummary(
                    row.Code,
                    row.RowsCount,
                    row.ErrorRowsCount,
                    row.WarningRowsCount))
            .ToArray();

        return new CatalogImportRowProblemsSummary(
            totalRowsCount,
            errorRowsCount,
            warningRowsCount,
            items);
    }

    public async Task<IReadOnlyCollection<CatalogImportManufacturerGroupSummary>> GetManufacturerGroupsAsync(
    Guid batchId,
    int take,
    CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(take, 1);

        var groups = await _dbContext.Database
            .SqlQuery<CatalogImportManufacturerGroupDbResult>(
                $"""
            WITH manufacturer_source_rows AS
            (
                SELECT
                    replace(
                        upper(
                            btrim(
                                rows.raw_data_json ->> manufacturer_column.source_column_number::text
                            )
                        ),
                        'Ё',
                        'Е'
                    ) AS group_key,
                    btrim(
                        rows.raw_data_json ->> manufacturer_column.source_column_number::text
                    ) AS source_value,
                    coalesce(
                        nullif(
                            rows.normalized_data_json ->> 'manufacturerResolutionSource',
                            ''
                        ),
                        CASE
                            WHEN nullif(
                                btrim(
                                    rows.normalized_data_json ->> 'manufacturerId'
                                ),
                                ''
                            ) IS NOT NULL
                            THEN 'Manual'
                            ELSE 'Unresolved'
                        END
                    ) AS resolution_source,
                    nullif(
                        btrim(
                            rows.normalized_data_json ->> 'manufacturer'
                        ),
                        ''
                    ) AS resolved_manufacturer_name
                FROM catalog_import_rows AS rows
                JOIN LATERAL
                (
                    SELECT import_column.source_column_number
                    FROM catalog_import_columns AS import_column
                    WHERE import_column.batch_id = rows.batch_id
                      AND import_column.target_kind = 'Manufacturer'
                    ORDER BY
                        import_column.is_confirmed DESC,
                        import_column.confidence DESC,
                        import_column.source_column_number
                    LIMIT 1
                ) AS manufacturer_column ON TRUE
                WHERE rows.batch_id = {batchId}
                  AND btrim(
                        coalesce(
                            rows.raw_data_json ->> manufacturer_column.source_column_number::text,
                            ''
                        )
                      ) <> ''
            )
            SELECT
                group_key AS "GroupKey",
                min(source_value) AS "SourceValue",
                CASE
                    WHEN count(DISTINCT resolution_source) = 1
                    THEN min(resolution_source)
                    ELSE 'Mixed'
                END AS "ResolutionSource",
                CASE
                    WHEN count(DISTINCT resolved_manufacturer_name)
                         FILTER (WHERE resolved_manufacturer_name IS NOT NULL) = 1
                    THEN min(resolved_manufacturer_name)
                    ELSE NULL
                END AS "ResolvedManufacturerName",
                count(*)
                    FILTER (WHERE resolution_source = 'ExactName')::integer
                    AS "ExactNameRowsCount",
                count(*)
                    FILTER (WHERE resolution_source = 'ApprovedAlias')::integer
                    AS "ApprovedAliasRowsCount",
                count(*)
                    FILTER (WHERE resolution_source = 'IgnoredNoise')::integer
                    AS "IgnoredNoiseRowsCount",
                count(*)
                    FILTER (WHERE resolution_source = 'Unresolved')::integer
                    AS "UnresolvedRowsCount",
                count(*)
                    FILTER (WHERE resolution_source = 'Manual')::integer
                    AS "ManualRowsCount",
                count(*)::integer AS "RowsCount"
            FROM manufacturer_source_rows
            GROUP BY group_key
            ORDER BY
                count(*) DESC,
                min(source_value)
            LIMIT {take}
            """)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return groups
            .Select(group =>
                new CatalogImportManufacturerGroupSummary(
                    group.GroupKey,
                    group.SourceValue,
                    group.ResolutionSource,
                    group.ResolvedManufacturerName,
                    group.ExactNameRowsCount,
                    group.ApprovedAliasRowsCount,
                    group.IgnoredNoiseRowsCount,
                    group.UnresolvedRowsCount,
                    group.ManualRowsCount,
                    group.RowsCount))
            .ToArray();
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken =
            default)
    {
        return _dbContext.SaveChangesAsync(
            cancellationToken);
    }

    private sealed record CatalogImportRowProblemCodeDbResult(
        string Code,
        int RowsCount,
        int ErrorRowsCount,
        int WarningRowsCount);

    private sealed record CatalogImportManufacturerGroupDbResult(
        string GroupKey,
        string SourceValue,
        string ResolutionSource,
        string? ResolvedManufacturerName,
        int ExactNameRowsCount,
        int ApprovedAliasRowsCount,
        int IgnoredNoiseRowsCount,
        int UnresolvedRowsCount,
        int ManualRowsCount,
        int RowsCount);
}