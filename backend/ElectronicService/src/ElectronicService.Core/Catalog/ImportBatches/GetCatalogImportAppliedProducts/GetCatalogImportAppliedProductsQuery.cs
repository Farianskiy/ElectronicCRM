namespace ElectronicService.Core.Catalog.ImportBatches.GetCatalogImportAppliedProducts;

public sealed record GetCatalogImportAppliedProductsQuery(
    Guid BatchId,
    Guid CurrentUserId,
    int Page,
    int PageSize);