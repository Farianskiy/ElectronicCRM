namespace ElectronicService.Core.Catalog.ImportBatches.GetCatalogImportBatchHistory;

public sealed record GetCatalogImportBatchHistoryResult(
    Guid BatchId,
    IReadOnlyCollection<CatalogImportBatchHistoryItemResult> Items);

public sealed record CatalogImportBatchHistoryItemResult(
    string EventType,
    DateTime OccurredAtUtc,
    Guid? ActorUserId,
    string? ActorDisplayName,
    string? ActorEmail,
    string? ActorUserType,
    string? Comment);