namespace ElectronicService.Core.Catalog.PriceCalculations.ChangeCatalogPriceCalculationLineComponentQuantity;

public sealed record ChangeCatalogPriceCalculationLineComponentQuantityCommand(
    Guid CalculationId,
    Guid LineId,
    Guid ComponentLineId,
    int QuantityPerUnit,
    Guid CurrentUserId);
