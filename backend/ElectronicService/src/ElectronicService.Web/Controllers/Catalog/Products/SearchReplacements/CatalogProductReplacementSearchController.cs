using ElectronicService.Contracts.Catalog.Products;
using ElectronicService.Contracts.Catalog.Products.Replacements;
using ElectronicService.Core.Catalog.Products.SearchProducts;
using ElectronicService.Core.Catalog.Products.SearchReplacements;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Products.SearchReplacements;

[ApiController]
[Route("api/catalog/products/replacements/search")]
public sealed class CatalogProductReplacementSearchController : ControllerBase
{
    private readonly SearchProductReplacementsQueryHandler _handler;

    public CatalogProductReplacementSearchController(
        SearchProductReplacementsQueryHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    [ProducesResponseType(typeof(SearchProductReplacementsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SearchProductReplacementsResponse>> SearchReplacements(
        [FromBody] SearchProductReplacementsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = new SearchProductReplacementsQuery(
            request.Search,
            request.ProductTypeCode,
            request.Manufacturer,
            request.Characteristics?
                .Select(characteristic => new SearchProductCharacteristicFilter(
                    characteristic.Code,
                    characteristic.Value))
                .ToList() ?? [],
            request.OnlyInStock,
            request.MinimumScore,
            request.Page,
            request.PageSize);

        var result = await _handler
            .Handle(query, cancellationToken)
            .ConfigureAwait(false);

        if (result is null)
        {
            return NotFound("Исходный товар для подбора замен не найден.");
        }

        return Ok(new SearchProductReplacementsResponse(
            new ProductListItemResponse(
                result.SourceProduct.Id,
                result.SourceProduct.Article,
                result.SourceProduct.Name,
                result.SourceProduct.ProductTypeCode,
                result.SourceProduct.ProductTypeName,
                result.SourceProduct.ManufacturerName,
                result.SourceProduct.PriceAmount,
                result.SourceProduct.PriceCurrency,
                result.SourceProduct.StockQuantity),
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