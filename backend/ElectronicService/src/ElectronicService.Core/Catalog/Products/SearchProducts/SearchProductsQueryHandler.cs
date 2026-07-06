using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Core.Catalog.Products.GetProducts;

namespace ElectronicService.Core.Catalog.Products.SearchProducts;

public sealed class SearchProductsQueryHandler
{
    private readonly ICatalogProductsReader _catalogProductsReader;

    public SearchProductsQueryHandler(ICatalogProductsReader catalogProductsReader)
    {
        _catalogProductsReader = catalogProductsReader;
    }

    public Task<CatalogProductsPageResult> Handle(
        SearchProductsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return _catalogProductsReader.SearchProductsAsync(
            query,
            cancellationToken);
    }
}