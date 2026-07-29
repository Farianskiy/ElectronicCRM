namespace ElectronicService.Contracts.Catalog.ImportBatches;

public sealed record GetMyCatalogImportBatchesResponse(
    IReadOnlyCollection<MyCatalogImportBatchItemResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record MyCatalogImportBatchItemResponse(
    Guid BatchId,
    Guid? ProductTypeId,
    string OriginalFileName,
    long FileSizeBytes,
    string Status,
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