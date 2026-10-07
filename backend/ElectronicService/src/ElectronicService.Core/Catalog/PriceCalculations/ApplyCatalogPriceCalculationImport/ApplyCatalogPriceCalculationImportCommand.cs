namespace ElectronicService.Core.Catalog.PriceCalculations.ApplyCatalogPriceCalculationImport;

public sealed record ApplyCatalogPriceCalculationImportCommand(
    Guid CalculationId,
    IReadOnlyList<ApplyCatalogPriceCalculationImportRow> Rows,
    IReadOnlyList<ApplyCatalogPriceCalculationComponentImportRow> ComponentRows,
    IReadOnlyList<ApplyCatalogPriceCalculationCharacteristicImportRow> CharacteristicRows,
    Guid CurrentUserId);

public sealed record ApplyCatalogPriceCalculationImportRow(
    CatalogPriceCalculationImportAction Action,
    Guid ProductId,
    Guid? ExistingLineId,
    decimal? Quantity);

public enum CatalogPriceCalculationImportAction
{
    Add = 0,
    UpdateQuantity = 1,
    Remove = 2
}

public sealed record ApplyCatalogPriceCalculationComponentImportRow(
    CatalogPriceCalculationImportAction Action,
    Guid MainLineId,
    Guid? ExistingComponentLineId,
    Guid NeedDefinitionId,
    Guid ComponentProductId,
    int? QuantityPerUnit);

public enum CatalogPriceCalculationCharacteristicImportAction
{
    Set = 0,
    Remove = 1
}

public sealed record ApplyCatalogPriceCalculationCharacteristicImportRow(
    CatalogPriceCalculationCharacteristicImportAction Action,
    Guid ProductId,
    string CharacteristicCode,
    string? Value);
