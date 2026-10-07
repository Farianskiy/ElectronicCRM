namespace ElectronicService.Contracts.Catalog.PriceCalculations;

public sealed record ApplyCatalogPriceCalculationImportRequest(
    IReadOnlyList<
        ApplyCatalogPriceCalculationImportRowRequest>? Rows,
    IReadOnlyList<
        ApplyCatalogPriceCalculationComponentImportRowRequest>? ComponentRows,
    IReadOnlyList<
        ApplyCatalogPriceCalculationCharacteristicImportRowRequest>? CharacteristicRows);

public sealed record ApplyCatalogPriceCalculationImportRowRequest(
    string Action,
    Guid ProductId,
    Guid? ExistingLineId,
    decimal? Quantity);

public sealed record ApplyCatalogPriceCalculationComponentImportRowRequest(
    string Action,
    Guid MainLineId,
    Guid? ExistingComponentLineId,
    Guid NeedDefinitionId,
    Guid ComponentProductId,
    int? QuantityPerUnit);

public sealed record ApplyCatalogPriceCalculationCharacteristicImportRowRequest(
    string Action,
    Guid ProductId,
    string CharacteristicCode,
    string? Value);
