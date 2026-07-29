using ElectronicService.Domain.Catalog.ImportBatches;

namespace ElectronicService.Core.Catalog.ImportBatches.GetMyCatalogImportBatches;

public sealed record GetMyCatalogImportBatchesResult(
    IReadOnlyCollection<MyCatalogImportBatchItemResult> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record MyCatalogImportBatchItemResult(
    Guid BatchId,
    Guid? ProductTypeId,
    string OriginalFileName,
    long FileSizeBytes,
    CatalogImportBatchStatus Status,
    int RowsCount,
    int ValidRowsCount,
    int ErrorRowsCount,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTime LastActivityAtUtc,
    DateTime? SubmittedAtUtc,
    DateTime? ChangesRequestedAtUtc,
    string? ChangesRequestComment,
    DateTime? RejectedAtUtc,
    string? RejectionReason,
    DateTime? AppliedAtUtc,
    uint Version,
    bool CanEdit,
    bool CanSubmit,
    bool CanApply,
    bool CanDelete);