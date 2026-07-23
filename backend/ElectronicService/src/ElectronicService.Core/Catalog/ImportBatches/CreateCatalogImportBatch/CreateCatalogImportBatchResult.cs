using ElectronicService.Domain.Catalog.ImportBatches;

namespace ElectronicService.Core.Catalog.ImportBatches.CreateCatalogImportBatch;

public sealed record CreateCatalogImportBatchResult(
    Guid BatchId,
    CatalogImportBatchStatus Status);