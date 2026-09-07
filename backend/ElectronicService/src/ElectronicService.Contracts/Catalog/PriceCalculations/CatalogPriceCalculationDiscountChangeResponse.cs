namespace ElectronicService.Contracts.Catalog.PriceCalculations;

public sealed record CatalogPriceCalculationDiscountChangeResponse(
    Guid CalculationId,
    Guid ManufacturerId,
    string ManufacturerName,
    decimal DiscountPercent,
    int AffectedLinesCount,
    decimal CalculationTotalAmount,
    IReadOnlyList<CatalogPriceCalculationRecalculatedLineResponse> Lines);

public sealed record CatalogPriceCalculationRecalculatedLineResponse(
    Guid LineId,
    Guid ProductId,
    decimal Quantity,
    decimal BasePriceAmount,
    decimal DiscountPercent,
    decimal ProjectPriceAmount,
    decimal TotalAmount);