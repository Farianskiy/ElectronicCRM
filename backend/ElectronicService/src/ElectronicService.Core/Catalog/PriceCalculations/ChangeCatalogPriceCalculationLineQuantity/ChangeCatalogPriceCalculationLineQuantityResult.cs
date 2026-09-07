namespace ElectronicService.Core.Catalog.PriceCalculations.ChangeCatalogPriceCalculationLineQuantity;

public sealed record ChangeCatalogPriceCalculationLineQuantityResult(
    Guid CalculationId,
    Guid LineId,
    decimal Quantity,
    decimal BasePriceAmount,
    decimal DiscountPercent,
    decimal ProjectPriceAmount,
    decimal LineTotalAmount,
    decimal CalculationTotalAmount);