using ElectronicService.Domain.Catalog.ImportBatches;

namespace ElectronicService.Core.Catalog.ImportBatches.GetMyCatalogImportBatches;

public sealed record GetMyCatalogImportBatchesQuery(
    Guid CurrentUserId,
    CatalogImportBatchStatus? Status,
    int Page,
    int PageSize);