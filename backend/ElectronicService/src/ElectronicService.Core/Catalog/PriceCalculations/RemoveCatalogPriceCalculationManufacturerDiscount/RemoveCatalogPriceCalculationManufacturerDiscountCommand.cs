namespace ElectronicService.Core.Catalog.PriceCalculations.RemoveCatalogPriceCalculationManufacturerDiscount;

public sealed record RemoveCatalogPriceCalculationManufacturerDiscountCommand(
    Guid CalculationId,
    Guid ManufacturerId,
    Guid CurrentUserId);