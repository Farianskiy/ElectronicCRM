namespace ElectronicService.Core.Catalog.PriceCalculations.ApplyCatalogPriceCalculationImport;

public sealed record ApplyCatalogPriceCalculationImportResult(
    Guid CalculationId,
    int AddedLinesCount,
    decimal CalculationTotalAmount);
