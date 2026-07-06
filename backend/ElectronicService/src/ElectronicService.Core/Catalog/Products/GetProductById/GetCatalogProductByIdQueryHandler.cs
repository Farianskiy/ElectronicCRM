using ElectronicService.Core.Catalog.Products.Abstractions;

namespace ElectronicService.Core.Catalog.Products.GetProductById;

public sealed class GetCatalogProductByIdQueryHandler
{
    private readonly ICatalogProductsReader _catalogProductsReader;

    public GetCatalogProductByIdQueryHandler(ICatalogProductsReader catalogProductsReader)
    {
        _catalogProductsReader = catalogProductsReader;
    }

    public Task<CatalogProductDetailsResult?> Handle(
        GetCatalogProductByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        return _catalogProductsReader.GetProductByIdAsync(
            query.ProductId,
            cancellationToken);
    }
}