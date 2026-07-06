using ElectronicService.Core.Catalog.Products.GetProductById;
using ElectronicService.Core.Catalog.Products.GetProducts;

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
}