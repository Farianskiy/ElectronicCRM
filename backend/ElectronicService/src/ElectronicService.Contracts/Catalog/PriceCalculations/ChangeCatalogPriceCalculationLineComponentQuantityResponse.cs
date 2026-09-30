namespace ElectronicService.Contracts.Catalog.PriceCalculations;

public sealed record ChangeCatalogPriceCalculationLineComponentQuantityResponse(
    Guid CalculationId,
    Guid LineId,
    Guid ComponentLineId,
    int QuantityPerUnit,
    decimal TotalQuantity,
    decimal BasePriceAmount,
    decimal DiscountPercent,
    decimal ProjectPriceAmount,
    decimal ComponentTotalAmount,
    decimal LineTotalAmount,
    decimal CalculationTotalAmount);
