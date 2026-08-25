namespace ElectronicService.Contracts.Catalog.ImportBatches;

public sealed record BulkUpdateCatalogImportRowsRequest(
    uint ExpectedVersion,
    IReadOnlyCollection<BulkUpdateCatalogImportRowRequest>? Rows);