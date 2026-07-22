using ElectronicService.Contracts.Catalog.Products.Management;
using ElectronicService.Core.Catalog.Products.SetCharacteristic;
using Microsoft.AspNetCore.Authorization;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Products.SetCharacteristic;

[Authorize(Roles = "Technical")]
[ApiController]
[Route("api/catalog/products/{id:guid}/characteristics")]
public sealed class CatalogProductCharacteristicController : ControllerBase
{
    private const string ProductNotFoundCode = "catalog.product.not_found";

    private const string ProductConcurrencyConflictCode =
    "catalog.product.concurrency_conflict";

    private readonly SetProductCharacteristicCommandHandler _handler;

    public CatalogProductCharacteristicController(
        SetProductCharacteristicCommandHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SetCharacteristic(
        Guid id,
        [FromBody] SetProductCharacteristicRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!User.TryGetUserId(
        out var changedByUserId))
        {
            return Unauthorized();
        }

        var command = new SetProductCharacteristicCommand(
            id,
            changedByUserId,
            request.Code,
            request.Value);

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