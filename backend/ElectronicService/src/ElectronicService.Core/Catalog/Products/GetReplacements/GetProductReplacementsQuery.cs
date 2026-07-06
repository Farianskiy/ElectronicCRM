namespace ElectronicService.Core.Catalog.Products.GetReplacements;

public sealed record GetProductReplacementsQuery(
    Guid ProductId,
    bool OnlyInStock,
    decimal MinimumScore,
    int Page,
    int PageSize);