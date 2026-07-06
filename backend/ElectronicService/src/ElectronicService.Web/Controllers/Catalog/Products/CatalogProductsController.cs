using ElectronicService.Contracts.Catalog.Products;
using ElectronicService.Core.Catalog.Products.GetProducts;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Products;

[ApiController]
[Route("api/catalog/products")]
public sealed class CatalogProductsController : ControllerBase
{
    private readonly GetCatalogProductsQueryHandler _getCatalogProductsQueryHandler;

    public CatalogProductsController(
        GetCatalogProductsQueryHandler getCatalogProductsQueryHandler)
    {
        _getCatalogProductsQueryHandler = getCatalogProductsQueryHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ProductsListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductsListResponse>> GetProducts(
        [FromQuery] string? search,
        [FromQuery] string? productTypeCode,
        [FromQuery] string? manufacturer,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetCatalogProductsQuery(
            search,
            productTypeCode,
            manufacturer,
            page,
            pageSize);

        var result = await _getCatalogProductsQueryHandler
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