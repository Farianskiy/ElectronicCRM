namespace ElectronicService.Contracts.Catalog.ImportBatches;

public sealed record GetCatalogImportBatchResponse(
    Guid BatchId,
    Guid CreatedByUserId,
    Guid? ProductTypeId,
    string OriginalFileName,
    long FileSizeBytes,
    string Status,
    int RowsCount,
    int ValidRowsCount,
    int ErrorRowsCount,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTime? SubmittedAtUtc,
    uint Version,
    bool CanEdit,
    bool CanSubmit,
    bool CanApply);