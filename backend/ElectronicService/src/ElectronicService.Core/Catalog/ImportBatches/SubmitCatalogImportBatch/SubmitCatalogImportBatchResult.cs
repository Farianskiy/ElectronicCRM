using ElectronicService.Domain.Catalog.ImportBatches;

namespace ElectronicService.Core.Catalog.ImportBatches.SubmitCatalogImportBatch;

public sealed record SubmitCatalogImportBatchResult(
    Guid BatchId,
    CatalogImportBatchStatus Status,
    DateTime? SubmittedAtUtc,
    uint Version);