namespace ElectronicService.Contracts.Catalog.Products;

public sealed record ProductsListResponse(
    IReadOnlyCollection<ProductListItemResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);