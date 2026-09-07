namespace ElectronicService.Contracts.Catalog.PriceCalculations;

public sealed record RemoveCatalogPriceCalculationLineResponse(
    Guid CalculationId,
    Guid RemovedLineId,
    int RemainingLinesCount,
    decimal CalculationTotalAmount);