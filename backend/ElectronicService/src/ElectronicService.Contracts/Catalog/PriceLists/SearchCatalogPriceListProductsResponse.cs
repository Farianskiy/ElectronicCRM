namespace ElectronicService.Contracts.Catalog.PriceLists;

public sealed record SearchCatalogPriceListProductsResponse(
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    IReadOnlyList<CatalogPriceListProductSearchResponse> Items);

public sealed record CatalogPriceListProductSearchResponse(
    Guid ProductId,
    string Article,
    string Name,
    string ProductTypeCode,
    string ProductTypeName);