namespace ElectronicService.Contracts.Catalog.ImportBatches;

public sealed record RejectCatalogImportBatchRequest(
    string? Reason);