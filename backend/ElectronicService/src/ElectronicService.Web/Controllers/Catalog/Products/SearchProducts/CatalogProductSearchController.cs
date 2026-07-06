using ElectronicService.Contracts.Catalog.Products;
using ElectronicService.Contracts.Catalog.Products.Search;
using ElectronicService.Core.Catalog.Products.SearchProducts;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Products;

[ApiController]
[Route("api/catalog/products/search")]
public sealed class CatalogProductSearchController : ControllerBase
{
    private readonly SearchProductsQueryHandler _searchProductsQueryHandler;

    public CatalogProductSearchController(SearchProductsQueryHandler searchProductsQueryHandler)
    {
        _searchProductsQueryHandler = searchProductsQueryHandler;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProductsListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductsListResponse>> SearchProducts(
        SearchProductsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = new SearchProductsQuery(
            request.Search,
            request.ProductTypeCode,
            request.Manufacturer,
            request.Characteristics?
                .Select(characteristic => new SearchProductCharacteristicFilter(
                    characteristic.Code,
                    characteristic.Value))
                .ToList() ?? [],
            request.Page,
            request.PageSize);

        var result = await _searchProductsQueryHandler
            .Handle(query, cancellationToken)
            .ConfigureAwait(false);

        return Ok(new ProductsListResponse(
            result.Items.Select(item => new ProductListItemResponse(
                item.Id,
                item.Article,
                item.Name,
                item.ProductTypeCode,
                item.ProductTypeName,
                item.ManufacturerName,
                item.PriceAmount,
                item.PriceCurrency,
                item.StockQuantity)).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount));
    }
}