namespace ElectronicService.Core.Catalog.PriceCalculations.RemoveCatalogPriceCalculationLineComponent;

public sealed record RemoveCatalogPriceCalculationLineComponentCommand(
    Guid CalculationId,
    Guid LineId,
    Guid ComponentLineId,
    Guid CurrentUserId);
