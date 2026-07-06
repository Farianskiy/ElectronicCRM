namespace ElectronicService.Core.Catalog.Products.GetProducts;

public sealed record CatalogProductsPageResult(
    IReadOnlyCollection<CatalogProductListItemResult> Items,
    int Page,
    int PageSize,
    int TotalCount);