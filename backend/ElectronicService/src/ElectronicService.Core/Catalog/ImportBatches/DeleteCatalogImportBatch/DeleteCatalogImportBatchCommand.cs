namespace ElectronicService.Core.Catalog.ImportBatches.DeleteCatalogImportBatch;

public sealed record DeleteCatalogImportBatchCommand(
    Guid BatchId,
    Guid CurrentUserId);