namespace ElectronicService.Core.Catalog.PriceCalculations.RemoveCatalogPriceCalculationLine;

public sealed record RemoveCatalogPriceCalculationLineResult(
    Guid CalculationId,
    Guid RemovedLineId,
    int RemainingLinesCount,
    decimal CalculationTotalAmount);