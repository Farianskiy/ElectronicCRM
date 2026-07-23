namespace ElectronicService.Core.Catalog
    .ImportBatches.AnalyzeCatalogImportBatch;

public sealed record AnalyzeCatalogImportBatchCommand(
    Guid BatchId,
    Guid CurrentUserId,
    Guid? ProductTypeId);