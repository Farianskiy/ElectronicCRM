namespace ElectronicService.Core.Catalog.PriceCalculations.Models;

public sealed record CatalogPriceCalculationRecalculatedLine(
    Guid LineId,
    Guid ProductId,
    decimal Quantity,
    decimal BasePriceAmount,
    decimal DiscountPercent,
    decimal ProjectPriceAmount,
    decimal TotalAmount);