namespace ElectronicService.Contracts.Catalog.PriceCalculations;

public sealed record GetMyCatalogPriceCalculationsResponse(
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    IReadOnlyList<CatalogPriceCalculationListItemResponse> Items);

public sealed record CatalogPriceCalculationListItemResponse(
    Guid CalculationId,
    string Title,
    string Currency,
    string Status,
    decimal TotalAmount,
    int LinesCount,
    int ManufacturersCount,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTime? CompletedAtUtc,
    DateTime? ArchivedAtUtc);