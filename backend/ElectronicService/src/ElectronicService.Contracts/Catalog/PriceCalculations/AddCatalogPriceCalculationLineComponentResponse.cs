namespace ElectronicService.Contracts.Catalog.PriceCalculations;

public sealed record AddCatalogPriceCalculationLineComponentResponse(
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
