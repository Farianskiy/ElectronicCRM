namespace ElectronicService.Core.Catalog.Products.GetProducts;

public sealed record GetCatalogProductsQuery(
    string? Search,
    string? ProductTypeCode,
    string? Manufacturer,
    int Page,
    int PageSize);