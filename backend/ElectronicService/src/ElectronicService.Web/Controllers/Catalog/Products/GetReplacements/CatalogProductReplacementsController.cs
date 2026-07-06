using ElectronicService.Contracts.Catalog.Products.Replacements;
using ElectronicService.Core.Catalog.Products.GetReplacements;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Products;

[ApiController]
[Route("api/catalog/products/{id:guid}/replacements")]
public sealed class CatalogProductReplacementsController : ControllerBase
{
    private readonly GetProductReplacementsQueryHandler _handler;

    public CatalogProductReplacementsController(
        GetProductReplacementsQueryHandler handler)
    {
        _handler = handler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ProductReplacementsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductReplacementsResponse>> GetReplacements(
        Guid id,
        [FromQuery] bool onlyInStock = false,
        [FromQuery] decimal minimumScore = 50,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetProductReplacementsQuery(
            id,
            onlyInStock,
            minimumScore,
            page,
            pageSize);

        var result = await _handler
            .Handle(query, cancellationToken)
            .ConfigureAwait(false);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(new ProductReplacementsResponse(
            result.ProductId,
            result.Items.Select(item => new ProductReplacementItemResponse(
                item.Id,
                item.Article,
                item.Name,
                item.ProductTypeCode,
                item.ProductTypeName,
                item.ManufacturerName,
                item.PriceAmount,
                item.PriceCurrency,
                item.StockQuantity,
                item.ReplacementScore)).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount));
    }
}