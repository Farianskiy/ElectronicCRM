namespace ElectronicService.Contracts.Catalog.PriceCalculations;

public sealed record AddCatalogPriceCalculationLineComponentRequest(
    Guid NeedDefinitionId,
    Guid ComponentProductId,
    int QuantityPerUnit);