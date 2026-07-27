using ElectronicService.Domain.Catalog.ImportBatches;

namespace ElectronicService.Core.Catalog
    .ImportBatches.GetCatalogImportBatch;

public sealed record GetCatalogImportBatchResult(
    Guid BatchId,
    Guid CreatedByUserId,
    Guid? ProductTypeId,
    string OriginalFileName,
    long FileSizeBytes,
    CatalogImportBatchStatus Status,
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