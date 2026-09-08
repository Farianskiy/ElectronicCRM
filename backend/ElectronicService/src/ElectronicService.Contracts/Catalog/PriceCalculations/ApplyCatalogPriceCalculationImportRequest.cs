namespace ElectronicService.Contracts.Catalog.PriceCalculations;

public sealed record ApplyCatalogPriceCalculationImportRequest(
    IReadOnlyList<
        ApplyCatalogPriceCalculationImportRowRequest>? Rows);

public sealed record ApplyCatalogPriceCalculationImportRowRequest(
    Guid ProductId,
    decimal Quantity);
