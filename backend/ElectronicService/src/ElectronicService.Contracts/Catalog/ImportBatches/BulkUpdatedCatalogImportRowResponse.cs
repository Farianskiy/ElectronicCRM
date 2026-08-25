namespace ElectronicService.Contracts.Catalog.ImportBatches;

public sealed record BulkUpdatedCatalogImportRowResponse(
    Guid RowId,
    int RowNumber,
    string RowStatus,
    CatalogImportNormalizedRowResponse Data,
    IReadOnlyCollection<CatalogImportRowIssueResponse> Issues,
    IReadOnlyCollection<CatalogImportRowIssueResponse> Warnings);