namespace ElectronicService.Contracts.Catalog.Import;

public sealed record PreviewProductsExcelImportResponse(
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
    IReadOnlyCollection<PreviewProductsExcelImportManufacturerNormalizationResponse> ManufacturerNormalizations,
    IReadOnlyCollection<PreviewProductsExcelImportRowResponse> Rows,
    IReadOnlyCollection<string> Errors);

public sealed record PreviewProductsExcelImportRowResponse(
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

public sealed record PreviewProductsExcelImportManufacturerNormalizationResponse(
    string RawName,
    string NormalizedName,
    int RowsCount);