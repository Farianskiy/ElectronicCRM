namespace ElectronicService.Core.Catalog.ImportBatches.ApplyCatalogImportBatch;

public sealed record ApplyCatalogImportBatchCommand(
    Guid BatchId,
    Guid CurrentUserId);