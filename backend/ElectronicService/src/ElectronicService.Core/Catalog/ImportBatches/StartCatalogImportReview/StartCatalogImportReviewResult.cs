using ElectronicService.Domain.Catalog.ImportBatches;

namespace ElectronicService.Core.Catalog.ImportBatches.StartCatalogImportReview;

public sealed record StartCatalogImportReviewResult(
    Guid BatchId,
    CatalogImportBatchStatus Status,
    Guid? ReviewedByUserId,
    DateTime? ReviewedAtUtc,
    uint Version);