namespace ElectronicService.Core.Catalog.PriceCalculations.AddCatalogPriceCalculationLine;

public sealed record AddCatalogPriceCalculationLineResult(
    Guid CalculationId,
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
    decimal BasePriceAmount,
    decimal? MrcPriceAmount,
    decimal DiscountPercent,
    decimal ProjectPriceAmount,
    decimal LineTotalAmount,
    decimal CalculationTotalAmount);