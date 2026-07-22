using ElectronicService.Contracts.Catalog.Products.Management;
using ElectronicService.Core.Catalog.Products.UpdateStock;
using Microsoft.AspNetCore.Authorization;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Products;

[Authorize(Roles = "Technical")]
[ApiController]
[Route("api/catalog/products/{id:guid}/stock")]
public sealed class CatalogProductStockController : ControllerBase
{
    private const string ProductNotFoundCode = "catalog.product.not_found";

    private const string ProductConcurrencyConflictCode =
    "catalog.product.concurrency_conflict";

    private readonly UpdateProductStockCommandHandler _handler;

    public CatalogProductStockController(UpdateProductStockCommandHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateStock(
        Guid id,
        UpdateProductStockRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!User.TryGetUserId(
        out var changedByUserId))
        {
            return Unauthorized();
        }

        var command = new UpdateProductStockCommand(
            id,
            changedByUserId,
            request.Quantity);

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

            if (string.Equals(
                result.Error.Code,
                ProductConcurrencyConflictCode,
                StringComparison.Ordinal))
            {
                return Conflict(
                    result.Error.Message);
            }

            return BadRequest(result.Error.Message);
        }

        return NoContent();
    }
}