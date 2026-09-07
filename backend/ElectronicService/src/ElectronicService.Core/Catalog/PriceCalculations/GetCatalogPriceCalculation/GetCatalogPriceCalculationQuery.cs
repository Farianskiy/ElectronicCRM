namespace ElectronicService.Core.Catalog.PriceCalculations.GetCatalogPriceCalculation;

public sealed record GetCatalogPriceCalculationQuery(
    Guid CalculationId,
    Guid CurrentUserId);