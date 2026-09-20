namespace ElectronicService.Core.Catalog.PriceCalculations.ApplyCatalogPriceCalculationImport;

public sealed record ApplyCatalogPriceCalculationImportCommand(
    Guid CalculationId,
    IReadOnlyList<ApplyCatalogPriceCalculationImportRow> Rows,
    Guid CurrentUserId);

public sealed record ApplyCatalogPriceCalculationImportRow(
    Guid ProductId,
    decimal Quantity);
