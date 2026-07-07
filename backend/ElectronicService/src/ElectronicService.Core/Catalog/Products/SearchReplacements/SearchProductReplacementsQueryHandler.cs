using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Core.Catalog.Products.GetReplacements;
using ElectronicService.Core.Catalog.Products.SearchProducts;

namespace ElectronicService.Core.Catalog.Products.SearchReplacements;

public sealed class SearchProductReplacementsQueryHandler
{
    private readonly ICatalogProductsReader _catalogProductsReader;
    private readonly ICatalogProductReplacementsReader _replacementsReader;

    public SearchProductReplacementsQueryHandler(
        ICatalogProductsReader catalogProductsReader,
        ICatalogProductReplacementsReader replacementsReader)
    {
        _catalogProductsReader = catalogProductsReader;
        _replacementsReader = replacementsReader;
    }

    public async Task<SearchProductReplacementsResult?> Handle(
        SearchProductReplacementsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sourceProducts = await _catalogProductsReader
            .SearchProductsAsync(
                new SearchProductsQuery(
                    query.Search,
                    query.ProductTypeCode,
                    query.Manufacturer,
                    query.Characteristics,
                    Page: 1,
                    PageSize: 1),
                cancellationToken)
            .ConfigureAwait(false);

        var sourceProduct = sourceProducts.Items.FirstOrDefault();

        if (sourceProduct is null)
        {
            return null;
        }

        var replacements = await _replacementsReader
            .GetReplacementsAsync(
                new GetProductReplacementsQuery(
                    sourceProduct.Id,
                    query.OnlyInStock,
                    query.MinimumScore,
                    query.Page,
                    query.PageSize),
                cancellationToken)
            .ConfigureAwait(false);

        if (replacements is null)
        {
            return null;
        }

        return new SearchProductReplacementsResult(
            sourceProduct,
            replacements.Items,
            replacements.Page,
            replacements.PageSize,
            replacements.TotalCount);
    }
}