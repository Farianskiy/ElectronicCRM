namespace ElectronicService.Contracts.Catalog.ImportBatches;

public sealed record UpdateCatalogImportRowResponse(
    Guid RowId,
    int RowNumber,
    string RowStatus,
    CatalogImportNormalizedRowResponse Data,
    IReadOnlyCollection<CatalogImportRowIssueResponse> Issues,
    IReadOnlyCollection<CatalogImportRowIssueResponse> Warnings,
    string BatchStatus,
    int RowsCount,
    int ValidRowsCount,
    int ErrorRowsCount,
    uint Version);