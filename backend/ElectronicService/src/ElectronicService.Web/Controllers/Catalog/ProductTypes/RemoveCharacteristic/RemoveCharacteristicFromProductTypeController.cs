using ElectronicService.Core.Catalog.ProductTypes
    .RemoveCharacteristic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog
    .ProductTypes.RemoveCharacteristic;

[Authorize(Roles = "Technical")]
[ApiController]
[Route(
    "api/catalog/product-types/{productTypeCode}/" +
    "characteristics/{characteristicDefinitionId:guid}")]
public sealed class
    RemoveCharacteristicFromProductTypeController
    : ControllerBase
{
    private const string ProductTypeNotFoundCode =
        "catalog.product_type.not_found";

    private const string
        ProductTypeCharacteristicNotFoundCode =
            "catalog.product_type_characteristic.not_found";

    private const string CannotBeRemovedCode =
        "catalog.product_type_characteristic.cannot_be_removed";

    private readonly
        RemoveCharacteristicFromProductTypeCommandHandler
        _handler;

    public RemoveCharacteristicFromProductTypeController(
        RemoveCharacteristicFromProductTypeCommandHandler
            handler)
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
    public async Task<IActionResult> RemoveCharacteristic(
        string productTypeCode,
        Guid characteristicDefinitionId,
        CancellationToken cancellationToken = default)
    {
        var command =
            new RemoveCharacteristicFromProductTypeCommand(
                productTypeCode,
                characteristicDefinitionId);

        var result = await _handler
            .Handle(command, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            if (string.Equals(
                    result.Error.Code,
                    ProductTypeNotFoundCode,
                    StringComparison.Ordinal)
                || string.Equals(
                    result.Error.Code,
                    ProductTypeCharacteristicNotFoundCode,
                    StringComparison.Ordinal))
            {
                return NotFound(
                    result.Error.Message);
            }

            if (string.Equals(
                    result.Error.Code,
                    CannotBeRemovedCode,
                    StringComparison.Ordinal))
            {
                return Conflict(
                    result.Error.Message);
            }

            return BadRequest(
                result.Error.Message);
        }

        return NoContent();
    }
}