namespace ElectronicService.Contracts.Catalog.PriceCalculations;

public sealed record SetCatalogPriceCalculationManufacturerDiscountRequest(
    decimal DiscountPercent);