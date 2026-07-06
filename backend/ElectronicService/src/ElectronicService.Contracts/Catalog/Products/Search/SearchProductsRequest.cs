namespace ElectronicService.Contracts.Catalog.Products.Search;

public sealed class SearchProductsRequest
{
    public string? Search { get; init; }

    public string? ProductTypeCode { get; init; }

    public string? Manufacturer { get; init; }

    public IReadOnlyCollection<SearchProductCharacteristicRequest>? Characteristics { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}