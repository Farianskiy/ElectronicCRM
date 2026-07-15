namespace ElectronicService.Core.Catalog.Import.PreviewProductsExcelImport;

public sealed record PreviewProductsExcelImportResult(
    string FileName,
    string ProductTypeCode,
    string ProductTypeName,
    int TotalRows,
    int CreateRows,
    int DuplicateRows,
    int ErrorRows,
    int NormalizedManufacturerRows,
    int NewManufacturerRows,
    int RowsLimit,
    bool IsRowsTruncated,
    IReadOnlyCollection<PreviewProductsExcelImportManufacturerNormalization> ManufacturerNormalizations,
    IReadOnlyCollection<PreviewProductsExcelImportRow> Rows,
    IReadOnlyCollection<string> Errors);