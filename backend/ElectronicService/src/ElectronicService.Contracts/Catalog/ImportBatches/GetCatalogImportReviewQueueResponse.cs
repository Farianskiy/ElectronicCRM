namespace ElectronicService.Contracts.Catalog.ImportBatches;

public sealed record GetCatalogImportReviewQueueResponse(
    IReadOnlyCollection<CatalogImportReviewQueueItemResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record CatalogImportReviewQueueItemResponse(
    Guid BatchId,
    Guid CreatedByUserId,
    string CreatedByDisplayName,
    string? CreatedByEmail,
    string CreatedByUserType,
    Guid? ProductTypeId,
    string OriginalFileName,
    string Status,
    int RowsCount,
    int ValidRowsCount,
    int ErrorRowsCount,
    DateTime CreatedAtUtc,
    DateTime? SubmittedAtUtc,
    Guid? ReviewedByUserId,
    DateTime? ReviewedAtUtc,
    uint Version);