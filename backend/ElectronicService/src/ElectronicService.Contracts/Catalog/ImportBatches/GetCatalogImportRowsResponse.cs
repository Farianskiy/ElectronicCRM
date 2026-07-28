namespace ElectronicService.Contracts.Catalog
    .ImportBatches;

public sealed record GetCatalogImportRowsResponse(
    IReadOnlyCollection<
        CatalogImportRowResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record CatalogImportRowResponse(
    Guid RowId,
    int RowNumber,
    string Status,
    IReadOnlyDictionary<int, string> RawData,
    CatalogImportNormalizedRowResponse Data,
    IReadOnlyCollection<
        CatalogImportRowIssueResponse> Issues,
    IReadOnlyCollection<
        CatalogImportRowIssueResponse> Warnings);

public sealed record CatalogImportNormalizedRowResponse(
    string? Name,
    string? Article,
    string? Manufacturer,
    Guid? ManufacturerId,
    decimal? Price,
    int? StockQuantity,
    IReadOnlyDictionary<string, string> Characteristics);

public sealed record CatalogImportRowIssueResponse(
    string Code,
    string Message,
    string? Field,
    int? SourceColumnNumber);