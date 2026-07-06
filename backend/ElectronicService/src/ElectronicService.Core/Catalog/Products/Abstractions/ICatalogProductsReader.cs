using ElectronicService.Core.Catalog.Products.GetProductById;
using ElectronicService.Core.Catalog.Products.GetProducts;
using ElectronicService.Core.Catalog.Products.SearchProducts;

namespace ElectronicService.Core.Catalog.Products.Abstractions;

public interface ICatalogProductsReader
{
    Task<CatalogProductsPageResult> GetProductsAsync(
        string? search,
        string? productTypeCode,
        string? manufacturer,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<CatalogProductDetailsResult?> GetProductByIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<CatalogProductsPageResult> SearchProductsAsync(
        SearchProductsQuery query,
        CancellationToken cancellationToken = default);
}