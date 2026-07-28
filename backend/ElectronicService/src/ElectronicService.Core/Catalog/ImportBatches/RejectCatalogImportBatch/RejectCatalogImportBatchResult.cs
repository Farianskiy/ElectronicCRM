using ElectronicService.Domain.Catalog.ImportBatches;

namespace ElectronicService.Core.Catalog.ImportBatches.RejectCatalogImportBatch;

public sealed record RejectCatalogImportBatchResult(
    Guid BatchId,
    CatalogImportBatchStatus Status,
    Guid? RejectedByUserId,
    DateTime? RejectedAtUtc,
    string? RejectionReason,
    uint Version);