namespace ElectronicService.Contracts.Catalog.PriceLists;

public sealed record GetCatalogPriceListVersionsResponse(
    Guid ManufacturerId,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    IReadOnlyList<CatalogPriceListVersionResponse> Items);

public sealed record CatalogPriceListVersionResponse(
    Guid PriceListId,
    string OriginalFileName,
    long FileSizeBytes,
    DateOnly? EffectiveDate,
    string Status,
    int RowsCount,
    int ValidRowsCount,
    int ErrorRowsCount,
    DateTime CreatedAtUtc,
    DateTime? ProcessedAtUtc,
    DateTime? ActivatedAtUtc,
    DateTime? ArchivedAtUtc,
    string? FailureReason);