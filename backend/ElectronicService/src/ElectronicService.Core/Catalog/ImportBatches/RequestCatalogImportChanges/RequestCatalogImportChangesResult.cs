using ElectronicService.Domain.Catalog.ImportBatches;

namespace ElectronicService.Core.Catalog.ImportBatches.RequestCatalogImportChanges;

public sealed record RequestCatalogImportChangesResult(
    Guid BatchId,
    CatalogImportBatchStatus Status,
    Guid? ChangesRequestedByUserId,
    DateTime? ChangesRequestedAtUtc,
    string? Comment,
    uint Version);