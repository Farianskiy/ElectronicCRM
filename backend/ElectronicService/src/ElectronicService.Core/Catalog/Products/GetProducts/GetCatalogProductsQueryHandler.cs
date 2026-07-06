using ElectronicService.Core.Catalog.Products.Abstractions;

namespace ElectronicService.Core.Catalog.Products.GetProducts;

public sealed class GetCatalogProductsQueryHandler
{
    private readonly ICatalogProductsReader _catalogProductsReader;

    public GetCatalogProductsQueryHandler(ICatalogProductsReader catalogProductsReader)
    {
        _catalogProductsReader = catalogProductsReader;
    }

    public Task<CatalogProductsPageResult> Handle(
        GetCatalogProductsQuery query,
        CancellationToken cancellationToken = default)
    {
        return _catalogProductsReader.GetProductsAsync(
            query.Search,
            query.ProductTypeCode,
            query.Manufacturer,
            query.Page,
            query.PageSize,
            cancellationToken);
    }
}