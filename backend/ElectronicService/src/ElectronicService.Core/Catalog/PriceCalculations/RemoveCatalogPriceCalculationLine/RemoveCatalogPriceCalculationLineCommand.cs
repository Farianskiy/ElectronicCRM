namespace ElectronicService.Core.Catalog.PriceCalculations.RemoveCatalogPriceCalculationLine;

public sealed record RemoveCatalogPriceCalculationLineCommand(
    Guid CalculationId,
    Guid LineId,
    Guid CurrentUserId);