namespace ElectronicService.Core.Catalog.Products.SearchProducts;

public sealed record SearchProductsQuery(
    string? Search,
    string? ProductTypeCode,
    string? Manufacturer,
    IReadOnlyCollection<SearchProductCharacteristicFilter> Characteristics,
    int Page,
    int PageSize,
    bool? OnlyInStock = null);