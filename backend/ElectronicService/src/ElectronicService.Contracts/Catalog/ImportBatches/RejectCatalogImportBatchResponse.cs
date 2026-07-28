namespace ElectronicService.Contracts.Catalog.ImportBatches;

public sealed record RejectCatalogImportBatchResponse(
    Guid BatchId,
    string Status,
    Guid? RejectedByUserId,
    DateTime? RejectedAtUtc,
    string? RejectionReason,
    uint Version);