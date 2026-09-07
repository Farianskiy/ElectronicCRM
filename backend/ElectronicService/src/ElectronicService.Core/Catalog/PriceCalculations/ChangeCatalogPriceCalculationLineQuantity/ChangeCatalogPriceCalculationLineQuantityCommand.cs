namespace ElectronicService.Core.Catalog.PriceCalculations.ChangeCatalogPriceCalculationLineQuantity;

public sealed record ChangeCatalogPriceCalculationLineQuantityCommand(
    Guid CalculationId,
    Guid LineId,
    decimal Quantity,
    Guid CurrentUserId);