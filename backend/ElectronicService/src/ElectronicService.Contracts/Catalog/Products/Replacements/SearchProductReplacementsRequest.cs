using ElectronicService.Contracts.Catalog.Products.Search;

namespace ElectronicService.Contracts.Catalog.Products.Replacements;

public sealed class SearchProductReplacementsRequest
{
    public string? Search { get; init; }

    public string? ProductTypeCode { get; init; }

    public string? Manufacturer { get; init; }

    public IReadOnlyCollection<SearchProductCharacteristicRequest>? Characteristics { get; init; }

    public bool OnlyInStock { get; init; }

    public decimal MinimumScore { get; init; } = 50;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}