using ElectronicService.Contracts.Catalog.Products;
using ElectronicService.Core.Catalog.Products.GetProductById;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Products;

[ApiController]
[Route("api/catalog/products/{id:guid}")]
public sealed class CatalogProductDetailsController : ControllerBase
{
    private readonly GetCatalogProductByIdQueryHandler _getCatalogProductByIdQueryHandler;

    public CatalogProductDetailsController(
        GetCatalogProductByIdQueryHandler getCatalogProductByIdQueryHandler)
    {
        _getCatalogProductByIdQueryHandler = getCatalogProductByIdQueryHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ProductDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDetailsResponse>> GetProductById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetCatalogProductByIdQuery(id);

        var result = await _getCatalogProductByIdQueryHandler
            .Handle(query, cancellationToken)
            .ConfigureAwait(false);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(new ProductDetailsResponse(
            result.Id,
            result.Article,
            result.Name,
            result.ProductTypeCode,
            result.ProductTypeName,
            result.ManufacturerName,
            result.PriceAmount,
            result.PriceCurrency,
            result.StockQuantity,
            result.Characteristics.Select(characteristic => new ProductCharacteristicResponse(
                characteristic.Code,
                characteristic.Name,
                characteristic.DataType,
                characteristic.Unit,
                characteristic.Value)).ToList(),
            result.Aliases));
    }
}