namespace ElectronicService.Core.Catalog.Import.PreviewProductsExcelImport;

public sealed record PreviewProductsExcelImportRow(
    int RowNumber,
    string Action,
    string? Article,
    string? Name,
    string ProductTypeCode,
    string RawManufacturerName,
    string NormalizedManufacturerName,
    string ManufacturerAction,
    IReadOnlyDictionary<string, string> Characteristics,
    IReadOnlyCollection<string> Warnings,
    IReadOnlyCollection<string> Errors);