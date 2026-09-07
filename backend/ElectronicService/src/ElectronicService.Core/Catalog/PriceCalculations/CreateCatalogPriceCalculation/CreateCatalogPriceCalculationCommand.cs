namespace ElectronicService.Core.Catalog.PriceCalculations.CreateCatalogPriceCalculation;

public sealed record CreateCatalogPriceCalculationCommand(
    Guid CurrentUserId,
    string Title);