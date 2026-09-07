namespace ElectronicService.Contracts.Catalog.PriceCalculations;

public sealed record AddCatalogPriceCalculationLineRequest(
    Guid ProductId,
    decimal Quantity);