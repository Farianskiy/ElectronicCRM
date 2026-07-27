using ElectronicService.Domain.Catalog
    .ImportBatches;

namespace ElectronicService.Core.Catalog
    .ImportBatches.GetCatalogImportRows;

public sealed record GetCatalogImportRowsQuery(
    Guid BatchId,
    Guid CurrentUserId,
    CatalogImportRowStatus? Status,
    int Page,
    int PageSize);