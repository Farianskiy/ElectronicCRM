namespace ElectronicService.Core.Catalog.Import.PreviewProductsExcelImport;

public sealed record PreviewProductsExcelImportManufacturerNormalization(
    string RawName,
    string NormalizedName,
    int RowsCount);