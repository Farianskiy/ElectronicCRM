namespace ElectronicService.Core.Catalog
    .ImportBatches.GetCatalogImportBatch;

public sealed record GetCatalogImportBatchQuery(
    Guid BatchId,
    Guid CurrentUserId);