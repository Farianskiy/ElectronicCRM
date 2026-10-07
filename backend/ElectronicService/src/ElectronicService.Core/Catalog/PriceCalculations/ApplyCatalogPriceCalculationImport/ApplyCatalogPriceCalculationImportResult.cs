namespace ElectronicService.Core.Catalog.PriceCalculations.ApplyCatalogPriceCalculationImport;

public sealed record ApplyCatalogPriceCalculationImportResult(
    Guid CalculationId,
    int AddedLinesCount,
    int UpdatedLinesCount,
    int RemovedLinesCount,
    int AddedComponentsCount,
    int UpdatedComponentsCount,
    int RemovedComponentsCount,
    int UpdatedCharacteristicsCount,
    int RemovedCharacteristicsCount,
    decimal CalculationTotalAmount);
