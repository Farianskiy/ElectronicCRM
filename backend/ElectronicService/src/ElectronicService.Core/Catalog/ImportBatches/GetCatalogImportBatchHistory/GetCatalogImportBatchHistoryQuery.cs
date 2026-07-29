namespace ElectronicService.Core.Catalog.ImportBatches.GetCatalogImportBatchHistory;

public sealed record GetCatalogImportBatchHistoryQuery(
    Guid BatchId,
    Guid CurrentUserId);