namespace ElectronicService.Core.Catalog.ImportBatches.SubmitCatalogImportBatch;

public sealed record SubmitCatalogImportBatchCommand(
    Guid BatchId,
    Guid CurrentUserId);