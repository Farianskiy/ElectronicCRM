namespace ElectronicService.Core.Catalog.Products.GetReplacements;

public sealed record CatalogProductReplacementsResult(
    Guid ProductId,
    IReadOnlyCollection<CatalogProductReplacementItemResult> Items,
    int Page,
    int PageSize,
    int TotalCount);