namespace ElectronicService.Contracts.Catalog.PriceCalculations;

public sealed record ChangeCatalogPriceCalculationLineQuantityResponse(
    Guid CalculationId,
    Guid LineId,
    decimal Quantity,
    decimal BasePriceAmount,
    decimal DiscountPercent,
    decimal ProjectPriceAmount,
    decimal LineTotalAmount,
    decimal CalculationTotalAmount);