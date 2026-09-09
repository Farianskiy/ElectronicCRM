namespace ElectronicService.Contracts.Catalog.PriceCalculations;

public sealed record GetCatalogPriceCalculationResponse(
    Guid CalculationId,
    Guid CreatedByUserId,
    string Title,
    string Currency,
    string? CustomerName,
    string? ObjectName,
    string? ProjectNumber,
    string? ResponsibleName,
    string? Comment,
    DateOnly? ValidUntil,
    string Status,
    decimal TotalAmount,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTime? CompletedAtUtc,
    DateTime? ArchivedAtUtc,
    IReadOnlyList<CatalogPriceCalculationLineResponse> Lines,
    IReadOnlyList<CatalogPriceCalculationDiscountResponse> ManufacturerDiscounts);

public sealed record CatalogPriceCalculationLineResponse(
    Guid LineId,
    Guid ProductId,
    Guid ManufacturerId,
    string ManufacturerName,
    Guid PriceListId,
    Guid PriceListRowId,
    string Article,
    string Name,
    string? Unit,
    decimal Quantity,
    decimal StockQuantity,
    decimal ShortageQuantity,
    decimal BasePriceAmount,
    decimal? MrcPriceAmount,
    decimal DiscountPercent,
    decimal ProjectPriceAmount,
    decimal TotalAmount,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record CatalogPriceCalculationDiscountResponse(
    Guid DiscountId,
    Guid ManufacturerId,
    string ManufacturerName,
    decimal DiscountPercent,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);