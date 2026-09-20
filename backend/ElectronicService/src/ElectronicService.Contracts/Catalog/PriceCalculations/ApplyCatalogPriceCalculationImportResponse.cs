namespace ElectronicService.Contracts.Catalog.PriceCalculations;

public sealed record ApplyCatalogPriceCalculationImportResponse(
    Guid CalculationId,
    int AddedLinesCount,
    decimal CalculationTotalAmount);
