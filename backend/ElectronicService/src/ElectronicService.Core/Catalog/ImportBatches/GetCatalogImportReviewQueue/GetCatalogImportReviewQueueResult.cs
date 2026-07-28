using ElectronicService.Domain.Catalog.ImportBatches;

namespace ElectronicService.Core.Catalog.ImportBatches.GetCatalogImportReviewQueue;

public sealed record GetCatalogImportReviewQueueResult(
    IReadOnlyCollection<CatalogImportReviewQueueItemResult> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record CatalogImportReviewQueueItemResult(
    Guid BatchId,
    Guid CreatedByUserId,
    string CreatedByDisplayName,
    string? CreatedByEmail,
    string CreatedByUserType,
    Guid? ProductTypeId,
    string OriginalFileName,
    CatalogImportBatchStatus Status,
    int RowsCount,
    int ValidRowsCount,
    int ErrorRowsCount,
    DateTime CreatedAtUtc,
    DateTime? SubmittedAtUtc,
    Guid? ReviewedByUserId,
    DateTime? ReviewedAtUtc,
    uint Version);