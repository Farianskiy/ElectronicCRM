using ElectronicService.Core.Catalog.Products.RemoveCharacteristic;
using Microsoft.AspNetCore.Authorization;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Products
    .RemoveCharacteristic;

[Authorize(Roles = "Technical")]
[ApiController]
[Route("api/catalog/products/{id:guid}/characteristics/{code}")]
public sealed class CatalogProductCharacteristicRemovalController
    : ControllerBase
{
    private const string ProductNotFoundCode =
        "catalog.product.not_found";

    private const string ProductTypeNotFoundCode =
        "catalog.product_type.not_found";

    private const string DefinitionNotFoundCode =
        "catalog.characteristic_definition.not_found";

    private const string ProductDoesNotHaveCharacteristicCode =
        "catalog.product_does_not_have_characteristic";

    private const string RequiredCharacteristicCode =
        "catalog.required_characteristic.cannot_be_removed";

    private const string ProductConcurrencyConflictCode =
    "catalog.product.concurrency_conflict";

    private readonly
        RemoveProductCharacteristicCommandHandler _handler;

    public CatalogProductCharacteristicRemovalController(
        RemoveProductCharacteristicCommandHandler handler)
    {
        _handler = handler;
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RemoveCharacteristic(
        Guid id,
        string code,
        CancellationToken cancellationToken = default)
    {
        if (!User.TryGetUserId(
        out var changedByUserId))
        {
            return Unauthorized();
        }

        var command = new RemoveProductCharacteristicCommand(
            id,
            changedByUserId,
            code);

        var result = await _handler
            .Handle(command, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            if (string.Equals(
                    result.Error.Code,
                    RequiredCharacteristicCode,
                    StringComparison.Ordinal))
            {
                return Conflict(result.Error.Message);
            }

            if (string.Equals(
                    result.Error.Code,
                    ProductNotFoundCode,
                    StringComparison.Ordinal)
                || string.Equals(
                    result.Error.Code,
                    ProductTypeNotFoundCode,
                    StringComparison.Ordinal)
                || string.Equals(
                    result.Error.Code,
                    DefinitionNotFoundCode,
                    StringComparison.Ordinal)
                || string.Equals(
                    result.Error.Code,
                    ProductDoesNotHaveCharacteristicCode,
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