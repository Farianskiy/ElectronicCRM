using ElectronicService.Domain.Catalog.ImportBatches;

namespace ElectronicService.Core.Catalog.ImportBatches.ApplyCatalogImportBatch;

public sealed record ApplyCatalogImportBatchResult(
    Guid BatchId,
    CatalogImportBatchStatus Status,
    Guid? AppliedByUserId,
    DateTime? AppliedAtUtc,
    int CreatedProductsCount,
    uint Version);