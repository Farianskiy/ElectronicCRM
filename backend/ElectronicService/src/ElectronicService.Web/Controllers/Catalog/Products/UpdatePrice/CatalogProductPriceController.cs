using ElectronicService.Contracts.Catalog.Products.Management;
using ElectronicService.Core.Catalog.Products.UpdatePrice;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Products;

[ApiController]
[Route("api/catalog/products/{id:guid}/price")]
public sealed class CatalogProductPriceController : ControllerBase
{
    private const string ProductNotFoundCode = "catalog.product.not_found";

    private readonly UpdateProductPriceCommandHandler _handler;

    public CatalogProductPriceController(UpdateProductPriceCommandHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePrice(
        Guid id,
        UpdateProductPriceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new UpdateProductPriceCommand(
            id,
            request.Amount,
            request.Currency);

        var result = await _handler
            .Handle(command, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            if (string.Equals(
                    result.Error.Code,
                    ProductNotFoundCode,
                    StringComparison.Ordinal))
            {
                return NotFound(result.Error.Message);
            }

            return BadRequest(result.Error.Message);
        }

        return NoContent();
    }
}