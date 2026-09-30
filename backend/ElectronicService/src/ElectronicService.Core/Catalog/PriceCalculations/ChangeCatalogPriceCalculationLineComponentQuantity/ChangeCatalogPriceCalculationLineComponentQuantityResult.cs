namespace ElectronicService.Core.Catalog.PriceCalculations.ChangeCatalogPriceCalculationLineComponentQuantity;

public sealed record ChangeCatalogPriceCalculationLineComponentQuantityResult(
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
