namespace ElectronicService.Contracts.Catalog.Products
    .AuditHistory;

public sealed record ProductAuditHistoryResponse(
    Guid ProductId,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    IReadOnlyCollection<
        ProductAuditHistoryItemResponse>
        Items);

public sealed record ProductAuditHistoryItemResponse(
    Guid Id,
    string Operation,
    string Source,
    Guid? SourceId,
    Guid? ChangedByUserId,
    DateTime ChangedAtUtc,
    IReadOnlyCollection<
        ProductAuditHistoryChangeResponse>
        Changes);

public sealed record ProductAuditHistoryChangeResponse(
    string Field,
    string Label,
    string? Before,
    string? After);