using ElectronicService.Domain.Catalog.PriceLists;

namespace ElectronicService.Core.Catalog.PriceLists.Abstractions;

public interface ICatalogPriceListReader
{
    Task<CatalogPriceListDetails?> GetByIdAsync(
        Guid priceListId,
        CancellationToken cancellationToken = default);

    Task<CatalogPriceListVersionsPage> GetVersionsAsync(
        Guid manufacturerId,
        CatalogPriceListStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<CatalogPriceListRowsPage> GetRowsAsync(
        Guid priceListId,
        CatalogPriceListRowStatus? status,
        CatalogPriceListRowMatchStatus? matchStatus,
        string? issueCode,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<CatalogPriceListIssueGroupsPage> GetIssueGroupsAsync(
        Guid priceListId,
        string? issueCode,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<CatalogPriceListProductsPage> SearchProductsAsync(
        Guid manufacturerId,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
}

public sealed record CatalogPriceListDetails(
    Guid PriceListId,
    Guid ManufacturerId,
    string ManufacturerName,
    Guid CreatedByUserId,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    string Currency,
    decimal VatRatePercent,
    DateOnly? EffectiveDate,
    CatalogPriceListStatus Status,
    int RowsCount,
    int ValidRowsCount,
    int ErrorRowsCount,
    int EstimatedRowsCount,
    int ReadRowsCount,
    int SavedRowsCount,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTime? ProcessedAtUtc,
    DateTime? ActivatedAtUtc,
    DateTime? ArchivedAtUtc,
    string? FailureReason);

public sealed record CatalogPriceListRowDetails(
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
    IReadOnlyList<CatalogPriceListRowIssue> Issues);

public sealed record CatalogPriceListRowsPage(
    int TotalCount,
    IReadOnlyList<CatalogPriceListRowDetails> Items);

public sealed record CatalogPriceListProductSearchItem(
    Guid ProductId,
    string Article,
    string Name,
    string ProductTypeCode,
    string ProductTypeName);

public sealed record CatalogPriceListProductsPage(
    int TotalCount,
    IReadOnlyList<CatalogPriceListProductSearchItem> Items);

public sealed record CatalogPriceListVersionItem(
    Guid PriceListId,
    string OriginalFileName,
    long FileSizeBytes,
    DateOnly? EffectiveDate,
    CatalogPriceListStatus Status,
    int RowsCount,
    int ValidRowsCount,
    int ErrorRowsCount,
    DateTime CreatedAtUtc,
    DateTime? ProcessedAtUtc,
    DateTime? ActivatedAtUtc,
    DateTime? ArchivedAtUtc,
    string? FailureReason);

public sealed record CatalogPriceListVersionsPage(
    int TotalCount,
    IReadOnlyList<CatalogPriceListVersionItem> Items);

public sealed record CatalogPriceListIssueGroupItem(
    string GroupKey,
    string IssueCode,
    string Field,
    string SourceValue,
    int RowsCount,
    IReadOnlyList<int> ExampleRowNumbers);

public sealed record CatalogPriceListIssueGroupsPage(
    int TotalCount,
    IReadOnlyList<CatalogPriceListIssueGroupItem> Items);