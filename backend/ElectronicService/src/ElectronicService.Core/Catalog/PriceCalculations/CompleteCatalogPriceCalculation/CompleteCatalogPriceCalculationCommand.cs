namespace ElectronicService.Core.Catalog.PriceCalculations.CompleteCatalogPriceCalculation;

public sealed record CompleteCatalogPriceCalculationCommand(
    Guid CalculationId,
    Guid CurrentUserId);