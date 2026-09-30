namespace ElectronicService.Contracts.Catalog.PriceCalculations;

public sealed record RemoveCatalogPriceCalculationLineComponentResponse(
    Guid CalculationId,
    Guid LineId,
    Guid RemovedComponentLineId,
    int RemainingComponentsCount,
    decimal LineTotalAmount,
    decimal CalculationTotalAmount);
