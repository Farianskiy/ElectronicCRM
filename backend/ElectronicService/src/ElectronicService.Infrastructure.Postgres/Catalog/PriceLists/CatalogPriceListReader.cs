using System.Text.Json;
using ElectronicService.Core.Catalog.PriceLists.Abstractions;
using ElectronicService.Domain.Catalog.PriceLists;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.PriceLists;

public sealed class CatalogPriceListReader : ICatalogPriceListReader
{
    private static readonly JsonSerializerOptions
        IssuesSerializerOptions =
            new(JsonSerializerDefaults.Web);

    private readonly ElectronicDbContext _dbContext;

    public CatalogPriceListReader(
        ElectronicDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public Task<CatalogPriceListDetails?>
        GetByIdAsync(
            Guid priceListId,
            CancellationToken cancellationToken = default)
    {
        return (
            from priceList in
                _dbContext.CatalogPriceLists.AsNoTracking()
            join manufacturer in
                _dbContext.Manufacturers.AsNoTracking()
                on priceList.ManufacturerId
                equals manufacturer.Id
            where priceList.Id == priceListId
            select new CatalogPriceListDetails(
                priceList.Id,
                priceList.ManufacturerId,
                manufacturer.Name,
                priceList.CreatedByUserId,
                priceList.OriginalFileName,
                priceList.ContentType,
                priceList.FileSizeBytes,
                priceList.Currency,
                priceList.VatRatePercent,
                priceList.EffectiveDate,
                priceList.Status,
                priceList.RowsCount,
                priceList.ValidRowsCount,
                priceList.ErrorRowsCount,
                priceList.EstimatedRowsCount,
                priceList.ReadRowsCount,
                priceList.SavedRowsCount,
                priceList.CreatedAtUtc,
                priceList.UpdatedAtUtc,
                priceList.ProcessedAtUtc,
                priceList.ActivatedAtUtc,
                priceList.ArchivedAtUtc,
                priceList.FailureReason))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<CatalogPriceListVersionsPage>
    GetVersionsAsync(
        Guid manufacturerId,
        CatalogPriceListStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query =
            _dbContext.CatalogPriceLists
                .AsNoTracking()
                .Where(
                    priceList =>
                        priceList.ManufacturerId
                        == manufacturerId);

        if (status.HasValue)
        {
            query =
                query.Where(
                    priceList =>
                        priceList.Status
                        == status.Value);
        }

        var totalCount =
            await query
                .CountAsync(cancellationToken)
                .ConfigureAwait(false);

        var items =
            await query
                .OrderByDescending(
                    priceList =>
                        priceList.CreatedAtUtc)
                .ThenByDescending(
                    priceList =>
                        priceList.Id)
                .Skip(skip)
                .Take(take)
                .Select(
                    priceList =>
                        new CatalogPriceListVersionItem(
                            priceList.Id,
                            priceList.OriginalFileName,
                            priceList.FileSizeBytes,
                            priceList.EffectiveDate,
                            priceList.Status,
                            priceList.RowsCount,
                            priceList.ValidRowsCount,
                            priceList.ErrorRowsCount,
                            priceList.CreatedAtUtc,
                            priceList.ProcessedAtUtc,
                            priceList.ActivatedAtUtc,
                            priceList.ArchivedAtUtc,
                            priceList.FailureReason))
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);

        return new CatalogPriceListVersionsPage(
            totalCount,
            items);
    }

    public async Task<CatalogPriceListRowsPage>
        GetRowsAsync(
            Guid priceListId,
            CatalogPriceListRowStatus? status,
            CatalogPriceListRowMatchStatus? matchStatus,
            string? issueCode,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
    {
        var query =
            _dbContext.CatalogPriceListRows
                .AsNoTracking()
                .Where(
                    row =>
                        row.PriceListId == priceListId);

        if (status.HasValue)
        {
            query =
                query.Where(
                    row =>
                        row.Status == status.Value);
        }

        if (matchStatus.HasValue)
        {
            query =
                query.Where(
                    row =>
                        row.MatchStatus
                        == matchStatus.Value);
        }

        if (!string.IsNullOrWhiteSpace(issueCode))
        {
            var issueFilterJson =
                JsonSerializer.Serialize(
                    new[]
                    {
                        new
                        {
                            code = issueCode.Trim()
                        }
                    });

            query =
                query.Where(
                    row =>
                        EF.Functions.JsonContains(
                            row.IssuesJson,
                            issueFilterJson));
        }

        var totalCount =
            await query
                .CountAsync(cancellationToken)
                .ConfigureAwait(false);

        var storedRows =
            await query
                .OrderBy(row =>
                    row.RowNumber)
                .Skip(skip)
                .Take(take)
                .Select(
                    row =>
                        new StoredPriceListRow(
                            row.Id,
                            row.RowNumber,
                            row.Article,
                            row.Name,
                            row.BasePriceAmount,
                            row.MrcPriceAmount,
                            row.ProductUrl,
                            row.Unit,
                            row.ProductId,
                            row.Status,
                            row.MatchStatus,
                            row.MatchConfidencePercent,
                            row.IssuesJson))
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);

        var items =
            storedRows
                .Select(
                    row =>
                        new CatalogPriceListRowDetails(
                            row.RowId,
                            row.RowNumber,
                            row.Article,
                            row.Name,
                            row.BasePriceAmount,
                            row.MrcPriceAmount,
                            row.ProductUrl,
                            row.Unit,
                            row.ProductId,
                            row.Status,
                            row.MatchStatus,
                            row.MatchConfidencePercent,
                            DeserializeIssues(
                                row.IssuesJson)))
                .ToArray();

        return new CatalogPriceListRowsPage(
            totalCount,
            items);
    }

    async Task<CatalogPriceListIssueGroupsPage>
        ICatalogPriceListReader.GetIssueGroupsAsync(
            Guid priceListId,
            string? issueCode,
            int skip,
            int take,
            CancellationToken cancellationToken)
    {
        var normalizedIssueCode =
            string.IsNullOrWhiteSpace(issueCode)
                ? null
                : issueCode.Trim().ToUpperInvariant();

        var storedGroups =
            await _dbContext.Database
                .SqlQuery<CatalogPriceListIssueGroupDbResult>(
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
                    grouped_issues AS
                    (
                        SELECT
                            md5(
                                concat_ws(
                                    chr(31),
                                    normalized_issue_code,
                                    normalized_issue_field,
                                    normalized_source_value
                                )
                            ) AS group_key,
                            min(issue_code) AS issue_code,
                            min(issue_field) AS issue_field,
                            min(source_value) AS source_value,
                            count(DISTINCT row_id)::integer AS rows_count,
                            (
                                array_agg(
                                    DISTINCT row_number
                                    ORDER BY row_number
                                )
                            )[1:5] AS example_row_numbers
                        FROM normalized_issues
                        WHERE (
                            CAST({normalizedIssueCode} AS text) IS NULL
                            OR normalized_issue_code =
                                {normalizedIssueCode}
                        )
                        GROUP BY
                            normalized_issue_code,
                            normalized_issue_field,
                            normalized_source_value
                    )
                    SELECT
                        group_key AS "GroupKey",
                        issue_code AS "IssueCode",
                        issue_field AS "Field",
                        source_value AS "SourceValue",
                        rows_count AS "RowsCount",
                        example_row_numbers AS "ExampleRowNumbers",
                        count(*) OVER()::integer AS "TotalCount"
                    FROM grouped_issues
                    ORDER BY
                        rows_count DESC,
                        issue_code,
                        issue_field,
                        source_value
                    OFFSET {skip}
                    LIMIT {take}
                    """)
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);

        var totalCount =
            storedGroups.Length == 0
                ? 0
                : storedGroups[0].TotalCount;

        var items =
            storedGroups
                .Select(
                    group =>
                        new CatalogPriceListIssueGroupItem(
                            group.GroupKey,
                            group.IssueCode,
                            group.Field,
                            group.SourceValue,
                            group.RowsCount,
                            group.ExampleRowNumbers))
                .ToArray();

        return new CatalogPriceListIssueGroupsPage(
            totalCount,
            items);
    }

    public async Task<CatalogPriceListProductsPage>
    SearchProductsAsync(
        Guid manufacturerId,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var productsQuery =
            from product in
                _dbContext.Products.AsNoTracking()
            join productType in
                _dbContext.ProductTypes.AsNoTracking()
                on product.ProductTypeId
                equals productType.Id
            where product.ManufacturerId
                  == manufacturerId
            select new
            {
                Product = product,
                ProductType = productType
            };

        var normalizedSearch =
            NormalizeSearch(search);

        if (normalizedSearch is not null)
        {
            var searchPattern =
                $"%{normalizedSearch}%";

            productsQuery =
                productsQuery.Where(
                    item =>
                        EF.Functions.ILike(
                            item.Product.Article.Value,
                            searchPattern)
                        || EF.Functions.ILike(
                            item.Product.Name.NormalizedValue,
                            searchPattern));
        }

        var totalCount =
            await productsQuery
                .CountAsync(cancellationToken)
                .ConfigureAwait(false);

        var itemsQuery =
            normalizedSearch is null
                ? productsQuery
                    .OrderBy(
                        item =>
                            item.Product.Name.NormalizedValue)
                    .ThenBy(
                        item =>
                            item.Product.Article.Value)
                : productsQuery
                    .OrderByDescending(
                        item =>
                            EF.Functions.ILike(
                                item.Product.Article.Value,
                                normalizedSearch))
                    .ThenBy(
                        item =>
                            item.Product.Name.NormalizedValue)
                    .ThenBy(
                        item =>
                            item.Product.Article.Value);

        var items =
            await itemsQuery
                .Skip(skip)
                .Take(take)
                .Select(
                    item =>
                        new CatalogPriceListProductSearchItem(
                            item.Product.Id,
                            item.Product.Article.Value,
                            item.Product.Name.Value,
                            item.ProductType.Code,
                            item.ProductType.Name))
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);

        return new CatalogPriceListProductsPage(
            totalCount,
            items);
    }

    private static string? NormalizeSearch(
        string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return null;
        }

        return search
            .Trim()
            .ToUpperInvariant()
            .Replace(
                "Ё",
                "Е",
                StringComparison.Ordinal);
    }

    private static CatalogPriceListRowIssue[]
        DeserializeIssues(
            string issuesJson)
    {
        if (string.IsNullOrWhiteSpace(issuesJson))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<
                       CatalogPriceListRowIssue[]>(
                       issuesJson,
                       IssuesSerializerOptions)
                   ?? [];
        }
        catch (JsonException)
        {
            return
            [
                new CatalogPriceListRowIssue(
                    "issues.invalid_json",
                    nameof(CatalogPriceListRow.IssuesJson),
                    "Сохранённые ошибки строки имеют некорректный формат.")
            ];
        }
    }

    private sealed record CatalogPriceListIssueGroupDbResult(
    string GroupKey,
    string IssueCode,
    string Field,
    string SourceValue,
    int RowsCount,
    int[] ExampleRowNumbers,
    int TotalCount);

    private sealed record StoredPriceListRow(
        Guid RowId,
        int RowNumber,
        string Article,
        string Name,
        decimal? BasePriceAmount,
        decimal? MrcPriceAmount,
        Uri? ProductUrl,
        string? Unit,
        Guid? ProductId,
        CatalogPriceListRowStatus Status,
        CatalogPriceListRowMatchStatus MatchStatus,
        decimal? MatchConfidencePercent,
        string IssuesJson);
}