using ElectronicService.Core.Catalog.Products.SearchProducts;

namespace ElectronicService.Core.Catalog.Products.SearchReplacements;

public sealed record SearchProductReplacementsQuery(
    string? Search,
    string? ProductTypeCode,
    string? Manufacturer,
    IReadOnlyCollection<SearchProductCharacteristicFilter> Characteristics,
    bool OnlyInStock,
    decimal MinimumScore,
    int Page,
    int PageSize);