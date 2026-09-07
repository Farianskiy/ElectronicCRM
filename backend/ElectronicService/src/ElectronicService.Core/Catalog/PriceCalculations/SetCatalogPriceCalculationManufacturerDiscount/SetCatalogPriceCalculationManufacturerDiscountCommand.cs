namespace ElectronicService.Core.Catalog.PriceCalculations.SetCatalogPriceCalculationManufacturerDiscount;

public sealed record SetCatalogPriceCalculationManufacturerDiscountCommand(
    Guid CalculationId,
    Guid ManufacturerId,
    decimal DiscountPercent,
    Guid CurrentUserId);