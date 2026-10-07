namespace ElectronicService.Contracts.Catalog.PriceCalculations;

public sealed record ApplyCatalogPriceCalculationImportResponse(
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
