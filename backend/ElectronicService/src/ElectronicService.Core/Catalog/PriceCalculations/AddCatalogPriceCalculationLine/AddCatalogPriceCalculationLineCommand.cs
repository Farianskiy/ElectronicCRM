namespace ElectronicService.Core.Catalog.PriceCalculations.AddCatalogPriceCalculationLine;

public sealed record AddCatalogPriceCalculationLineCommand(
    Guid CalculationId,
    Guid ProductId,
    decimal Quantity,
    Guid CurrentUserId);