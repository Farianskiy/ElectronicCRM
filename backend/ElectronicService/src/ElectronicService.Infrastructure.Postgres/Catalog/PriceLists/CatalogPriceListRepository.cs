using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.PriceLists.Abstractions;
using ElectronicService.Domain.Catalog.PriceLists;
using ElectronicService.Domain.Common;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.PriceLists;

public sealed class CatalogPriceListRepository
    : ICatalogPriceListRepository
{
    private readonly ElectronicDbContext _dbContext;

    public CatalogPriceListRepository(
        ElectronicDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public void Add(
        CatalogPriceList priceList)
    {
        ArgumentNullException.ThrowIfNull(priceList);

        _dbContext.CatalogPriceLists.Add(priceList);
    }

    public Task<CatalogPriceList?> GetByIdAsync(
        Guid priceListId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.CatalogPriceLists
            .SingleOrDefaultAsync(
                priceList =>
                    priceList.Id == priceListId,
                cancellationToken);
    }

    public Task<CatalogPriceListRow?> GetRowByIdAsync(
        Guid priceListId,
        Guid rowId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.CatalogPriceListRows
            .SingleOrDefaultAsync(
                row =>
                    row.PriceListId == priceListId
                    && row.Id == rowId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<CatalogPriceListRow>>
    GetRowsByIdsAsync(
        Guid priceListId,
        IReadOnlyCollection<Guid> rowIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rowIds);

        return await _dbContext.CatalogPriceListRows
            .Where(
                row =>
                    row.PriceListId == priceListId
                    && rowIds.Contains(row.Id))
            .OrderBy(row =>
                row.RowNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyCollection<Guid>>
        GetProductIdsByManufacturerAsync(
            Guid manufacturerId,
            IReadOnlyCollection<Guid> productIds,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(productIds);

        return await _dbContext.Products
            .AsNoTracking()
            .Where(
                product =>
                    product.ManufacturerId == manufacturerId
                    && productIds.Contains(product.Id))
            .Select(product =>
                product.Id)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<bool> ProductBelongsToManufacturerAsync(
        Guid productId,
        Guid manufacturerId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Products
            .AsNoTracking()
            .AnyAsync(
                product =>
                    product.Id == productId
                    && product.ManufacturerId
                        == manufacturerId,
                cancellationToken);
    }

    public async Task<CatalogPriceListIssueGroupBatch?>
    GetIssueGroupBatchAsync(
        Guid priceListId,
        string groupKey,
        int take,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupKey);

        var normalizedGroupKey =
            groupKey.Trim().ToUpperInvariant();

        var storedRows =
            await _dbContext.Database
                .SqlQuery<CatalogPriceListIssueGroupRowDbResult>(
                    $"""
                WITH expanded_issues AS
                (
                    SELECT
                        rows.id AS row_id,
                        rows.row_number,
                        issue.value ->> 'code' AS issue_code,
                        issue.value ->> 'field' AS issue_field,
                        CASE lower(
                            COALESCE(
                                issue.value ->> 'field',
                                ''
                            )
                        )
                            WHEN 'article'
                                THEN rows.article
                            WHEN 'name'
                                THEN rows.name
                            WHEN 'basepriceamount'
                                THEN COALESCE(
                                    rows.base_price_amount::text,
                                    ''
                                )
                            WHEN 'mrcpriceamount'
                                THEN COALESCE(
                                    rows.mrc_price_amount::text,
                                    ''
                                )
                            WHEN 'producturl'
                                THEN COALESCE(
                                    rows.product_url,
                                    ''
                                )
                            WHEN 'unit'
                                THEN COALESCE(
                                    rows.unit,
                                    ''
                                )
                            WHEN 'productid'
                                THEN COALESCE(
                                    NULLIF(
                                        btrim(rows.article),
                                        ''
                                    ),
                                    rows.name,
                                    ''
                                )
                            ELSE ''
                        END AS source_value
                    FROM catalog_price_list_rows AS rows
                    CROSS JOIN LATERAL jsonb_array_elements(
                        rows.issues_json
                    ) AS issue(value)
                    WHERE rows.price_list_id = {priceListId}
                      AND rows.status = 'Error'
                ),
                normalized_issues AS
                (
                    SELECT
                        row_id,
                        row_number,
                        issue_code,
                        issue_field,
                        source_value,
                        upper(
                            btrim(
                                COALESCE(
                                    issue_code,
                                    ''
                                )
                            )
                        ) AS normalized_issue_code,
                        lower(
                            btrim(
                                COALESCE(
                                    issue_field,
                                    ''
                                )
                            )
                        ) AS normalized_issue_field,
                        upper(
                            regexp_replace(
                                btrim(
                                    COALESCE(
                                        source_value,
                                        ''
                                    )
                                ),
                                '\s+',
                                ' ',
                                'g'
                            )
                        ) AS normalized_source_value
                    FROM expanded_issues
                    WHERE issue_code IS NOT NULL
                      AND btrim(issue_code) <> ''
                ),
                matched_rows AS
                (
                    SELECT DISTINCT ON (row_id)
                        row_id,
                        row_number,
                        issue_code,
                        issue_field,
                        source_value
                    FROM normalized_issues
                    WHERE upper(
                        md5(
                            concat_ws(
                                chr(31),
                                normalized_issue_code,
                                normalized_issue_field,
                                normalized_source_value
                            )
                        )
                    ) = {normalizedGroupKey}
                    ORDER BY
                        row_id,
                        issue_code,
                        issue_field
                )
                SELECT
                    row_id AS "RowId",
                    issue_code AS "IssueCode",
                    issue_field AS "Field",
                    source_value AS "SourceValue"
                FROM matched_rows
                ORDER BY
                    row_number,
                    row_id
                LIMIT {take}
                """)
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);

        if (storedRows.Length == 0)
        {
            return null;
        }

        var firstRow =
            storedRows[0];

        return new CatalogPriceListIssueGroupBatch(
            firstRow.IssueCode,
            firstRow.Field,
            firstRow.SourceValue,
            storedRows
                .Select(row => row.RowId)
                .ToArray());
    }

    public async Task<UnitResult<DomainError>>
        SaveCorrectionBatchAsync(
            IReadOnlyCollection<CatalogPriceListRow> rows,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rows);

        try
        {
            await _dbContext
                .SaveChangesAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var row in rows)
            {
                _dbContext.Entry(row).State =
                    EntityState.Detached;
            }

            return UnitResult.Success<DomainError>();
        }
        catch (DbUpdateConcurrencyException)
        {
            return UnitResult.Failure(
                CatalogPriceListErrors.RowUpdateFailed());
        }
        catch (DbUpdateException)
        {
            return UnitResult.Failure(
                CatalogPriceListErrors.RowUpdateFailed());
        }
    }

    public async Task<Result<Guid?, DomainError>>
    ActivateAsync(
        CatalogPriceList priceList,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(priceList);

        await using var transaction =
            await _dbContext.Database
                .BeginTransactionAsync(cancellationToken)
                .ConfigureAwait(false);

        try
        {
            var activePriceList =
                await _dbContext.CatalogPriceLists
                    .SingleOrDefaultAsync(
                        existingPriceList =>
                            existingPriceList.ManufacturerId
                                == priceList.ManufacturerId
                            && existingPriceList.Status
                                == CatalogPriceListStatus.Active
                            && existingPriceList.Id
                                != priceList.Id,
                        cancellationToken)
                    .ConfigureAwait(false);

            Guid? archivedPriceListId = null;

            if (activePriceList is not null)
            {
                var archiveResult =
                    activePriceList.Archive();

                if (archiveResult.IsFailure)
                {
                    await transaction
                        .RollbackAsync(cancellationToken)
                        .ConfigureAwait(false);

                    return Result.Failure<Guid?, DomainError>(
                        archiveResult.Error);
                }

                archivedPriceListId =
                    activePriceList.Id;

                await _dbContext
                    .SaveChangesAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

            var activateResult =
                priceList.Activate();

            if (activateResult.IsFailure)
            {
                await transaction
                    .RollbackAsync(cancellationToken)
                    .ConfigureAwait(false);

                return Result.Failure<Guid?, DomainError>(
                    activateResult.Error);
            }

            await _dbContext
                .SaveChangesAsync(cancellationToken)
                .ConfigureAwait(false);

            await transaction
                .CommitAsync(cancellationToken)
                .ConfigureAwait(false);

            return Result.Success<Guid?, DomainError>(
                archivedPriceListId);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction
                .RollbackAsync(cancellationToken)
                .ConfigureAwait(false);

            return Result.Failure<Guid?, DomainError>(
                CatalogPriceListErrors
                    .PriceListActivationFailed());
        }
        catch (DbUpdateException)
        {
            await transaction
                .RollbackAsync(cancellationToken)
                .ConfigureAwait(false);

            return Result.Failure<Guid?, DomainError>(
                CatalogPriceListErrors
                    .PriceListActivationFailed());
        }
    }

    public async Task<UnitResult<DomainError>>
        SaveCorrectionAndRefreshStatisticsAsync(
            CatalogPriceList priceList,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(priceList);

        await using var transaction =
            await _dbContext.Database
                .BeginTransactionAsync(cancellationToken)
                .ConfigureAwait(false);

        try
        {
            await _dbContext
                .SaveChangesAsync(cancellationToken)
                .ConfigureAwait(false);

            var rowsCount =
                await _dbContext.CatalogPriceListRows
                    .CountAsync(
                        row =>
                            row.PriceListId
                            == priceList.Id,
                        cancellationToken)
                    .ConfigureAwait(false);

            var validRowsCount =
                await _dbContext.CatalogPriceListRows
                    .CountAsync(
                        row =>
                            row.PriceListId
                                == priceList.Id
                            && row.Status
                                == CatalogPriceListRowStatus.Valid,
                        cancellationToken)
                    .ConfigureAwait(false);

            var errorRowsCount =
                rowsCount - validRowsCount;

            var statisticsResult =
                priceList.RefreshRowsStatistics(
                    rowsCount,
                    validRowsCount,
                    errorRowsCount);

            if (statisticsResult.IsFailure)
            {
                await transaction
                    .RollbackAsync(cancellationToken)
                    .ConfigureAwait(false);

                return UnitResult.Failure(
                    statisticsResult.Error);
            }

            await _dbContext
                .SaveChangesAsync(cancellationToken)
                .ConfigureAwait(false);

            await transaction
                .CommitAsync(cancellationToken)
                .ConfigureAwait(false);

            return UnitResult.Success<DomainError>();
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction
                .RollbackAsync(cancellationToken)
                .ConfigureAwait(false);

            return UnitResult.Failure(
                CatalogPriceListErrors.RowUpdateFailed());
        }
        catch (DbUpdateException)
        {
            await transaction
                .RollbackAsync(cancellationToken)
                .ConfigureAwait(false);

            return UnitResult.Failure(
                CatalogPriceListErrors.RowUpdateFailed());
        }
    }

    private sealed record CatalogPriceListIssueGroupRowDbResult(
    Guid RowId,
    string IssueCode,
    string Field,
    string SourceValue);
}