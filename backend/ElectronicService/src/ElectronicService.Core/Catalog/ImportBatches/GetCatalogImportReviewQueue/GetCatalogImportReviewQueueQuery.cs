using ElectronicService.Domain.Catalog.ImportBatches;

namespace ElectronicService.Core.Catalog.ImportBatches.GetCatalogImportReviewQueue;

public sealed record GetCatalogImportReviewQueueQuery(
    Guid CurrentUserId,
    CatalogImportBatchStatus? Status,
    int Page,
    int PageSize);