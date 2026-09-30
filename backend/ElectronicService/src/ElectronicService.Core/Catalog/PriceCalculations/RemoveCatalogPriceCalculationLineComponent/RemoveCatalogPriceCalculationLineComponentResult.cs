namespace ElectronicService.Core.Catalog.PriceCalculations.RemoveCatalogPriceCalculationLineComponent;

public sealed record RemoveCatalogPriceCalculationLineComponentResult(
    Guid CalculationId,
    Guid LineId,
    Guid RemovedComponentLineId,
    int RemainingComponentsCount,
    decimal LineTotalAmount,
    decimal CalculationTotalAmount);
