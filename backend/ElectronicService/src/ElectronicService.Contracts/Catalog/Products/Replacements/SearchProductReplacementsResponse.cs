using ElectronicService.Contracts.Catalog.Products;

namespace ElectronicService.Contracts.Catalog.Products.Replacements;

public sealed record SearchProductReplacementsResponse(
    ProductListItemResponse SourceProduct,
    IReadOnlyCollection<ProductReplacementItemResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);