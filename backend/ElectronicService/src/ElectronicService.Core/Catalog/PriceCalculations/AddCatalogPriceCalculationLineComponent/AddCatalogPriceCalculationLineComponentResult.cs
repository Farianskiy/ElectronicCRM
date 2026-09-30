namespace ElectronicService.Core.Catalog.PriceCalculations.AddCatalogPriceCalculationLineComponent;

public sealed record AddCatalogPriceCalculationLineComponentResult(
    Guid CalculationId,
    Guid LineId,
    Guid ComponentLineId,
    Guid NeedDefinitionId,
    string NeedName,
    Guid ComponentProductId,
    Guid ManufacturerId,
    string ManufacturerName,
    string Article,
    string Name,
    int QuantityPerUnit,
    decimal BasePriceAmount,
    decimal DiscountPercent,
    decimal ProjectPriceAmount,
    decimal TotalQuantity,
    decimal ComponentTotalAmount,
    decimal LineTotalAmount,
    decimal CalculationTotalAmount);