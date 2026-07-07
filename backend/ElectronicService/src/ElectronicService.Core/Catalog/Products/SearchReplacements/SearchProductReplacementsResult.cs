using ElectronicService.Core.Catalog.Products.GetProducts;
using ElectronicService.Core.Catalog.Products.GetReplacements;

namespace ElectronicService.Core.Catalog.Products.SearchReplacements;

public sealed record SearchProductReplacementsResult(
    CatalogProductListItemResult SourceProduct,
    IReadOnlyCollection<CatalogProductReplacementItemResult> Items,
    int Page,
    int PageSize,
    int TotalCount);