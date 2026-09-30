namespace ElectronicService.Contracts.Catalog.Products.Management;

public sealed record BulkUpdateCatalogProductsResponse(
    int UpdatedProductsCount,
    IReadOnlyCollection<Guid> UpdatedProductIds);
