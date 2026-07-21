using ElectronicService.Contracts.Catalog
    .ProductTypes.Management;
using ElectronicService.Core.Catalog.ProductTypes
    .AddOptionalCharacteristic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog
    .ProductTypes.AddOptionalCharacteristic;

[Authorize(Roles = "Technical")]
[ApiController]
[Route(
    "api/catalog/product-types/{productTypeCode}/" +
    "characteristics")]
public sealed class
    AddOptionalCharacteristicToProductTypeController
    : ControllerBase
{
    private const string ProductTypeNotFoundCode =
        "catalog.product_type.not_found";

    private const string DefinitionNotFoundCode =
        "catalog.characteristic_definition.not_found";

    private const string AlreadyAddedCode =
        "catalog.characteristic_already_added_to_product_type";

    private readonly
        AddOptionalCharacteristicToProductTypeCommandHandler
        _handler;

    public AddOptionalCharacteristicToProductTypeController(
        AddOptionalCharacteristicToProductTypeCommandHandler
            handler)
    {
        _handler = handler;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddCharacteristic(
        string productTypeCode,
        [FromBody]
        AddOptionalCharacteristicToProductTypeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command =
            new AddOptionalCharacteristicToProductTypeCommand(
                productTypeCode,
                request.CharacteristicDefinitionId);

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
                    DefinitionNotFoundCode,
                    StringComparison.Ordinal))
            {
                return NotFound(
                    result.Error.Message);
            }

            if (string.Equals(
                    result.Error.Code,
                    AlreadyAddedCode,
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