namespace ElectronicService.Contracts.Catalog.ImportBatches;

public sealed record GetCatalogImportBatchHistoryResponse(
    Guid BatchId,
    IReadOnlyCollection<CatalogImportBatchHistoryItemResponse> Items);

public sealed record CatalogImportBatchHistoryItemResponse(
    string EventType,
    DateTime OccurredAtUtc,
    Guid? ActorUserId,
    string? ActorDisplayName,
    string? ActorEmail,
    string? ActorUserType,
    string? Comment);