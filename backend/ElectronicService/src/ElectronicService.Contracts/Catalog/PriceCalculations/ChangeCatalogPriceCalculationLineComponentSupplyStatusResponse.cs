namespace ElectronicService.Contracts.Catalog.PriceCalculations;

public sealed record ChangeCatalogPriceCalculationLineComponentSupplyStatusResponse(
    Guid CalculationId,
    Guid LineId,
    Guid ComponentLineId,
    string SupplyStatus,
    decimal ComponentTotalAmount,
    decimal LineTotalAmount,
    decimal CalculationTotalAmount);
