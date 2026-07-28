namespace ElectronicService.Core.Catalog.ImportBatches.RejectCatalogImportBatch;

public sealed record RejectCatalogImportBatchCommand(
    Guid BatchId,
    Guid CurrentUserId,
    string? Reason);