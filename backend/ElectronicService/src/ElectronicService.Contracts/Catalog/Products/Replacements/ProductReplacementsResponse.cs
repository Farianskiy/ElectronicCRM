namespace ElectronicService.Contracts.Catalog.Products.Replacements;

public sealed record ProductReplacementsResponse(
    Guid ProductId,
    IReadOnlyCollection<ProductReplacementItemResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);