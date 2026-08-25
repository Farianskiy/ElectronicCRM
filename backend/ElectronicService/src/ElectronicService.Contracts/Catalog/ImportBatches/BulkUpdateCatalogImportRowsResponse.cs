namespace ElectronicService.Contracts.Catalog.ImportBatches;

public sealed record BulkUpdateCatalogImportRowsResponse(
    IReadOnlyCollection<BulkUpdatedCatalogImportRowResponse> Rows,
    string BatchStatus,
    int RowsCount,
    int ValidRowsCount,
    int ErrorRowsCount,
    uint Version);