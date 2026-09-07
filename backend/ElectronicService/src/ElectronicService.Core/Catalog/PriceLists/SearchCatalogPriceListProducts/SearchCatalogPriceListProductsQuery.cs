namespace ElectronicService.Core.Catalog.PriceLists.SearchCatalogPriceListProducts;

public sealed record SearchCatalogPriceListProductsQuery(
    Guid PriceListId,
    Guid CurrentUserId,
    string? Search,
    int Page,
    int PageSize);