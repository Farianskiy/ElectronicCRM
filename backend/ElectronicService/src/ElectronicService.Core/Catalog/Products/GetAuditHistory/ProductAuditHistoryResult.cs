namespace ElectronicService.Core.Catalog.Products
    .GetAuditHistory;

public sealed record ProductAuditHistoryPageResult(
    Guid ProductId,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    IReadOnlyCollection<
        ProductAuditHistoryItemResult>
        Items);

public sealed record ProductAuditHistoryItemResult(
    Guid Id,
    string Operation,
    string Source,
    Guid? SourceId,
    Guid? ChangedByUserId,
    DateTime ChangedAtUtc,
    IReadOnlyCollection<
        ProductAuditHistoryChangeResult>
        Changes);

public sealed record ProductAuditHistoryChangeResult(
    string Field,
    string Label,
    string? Before,
    string? After);