namespace ElectronicService.Core.Catalog.Products.BulkUpdateProducts;

public sealed record BulkUpdateCatalogProductsResult(
    IReadOnlyCollection<Guid> UpdatedProductIds);
