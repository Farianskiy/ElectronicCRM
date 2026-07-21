using ElectronicService.Contracts.Catalog
    .ProductTypes.Management;
using ElectronicService.Core.Catalog.ProductTypes
    .SetCharacteristicRequired;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog
    .ProductTypes.SetCharacteristicRequired;

[Authorize(Roles = "Technical")]
[ApiController]
[Route(
    "api/catalog/product-types/{productTypeCode}/" +
    "characteristics/{characteristicDefinitionId:guid}/" +
    "required")]
public sealed class
    SetProductTypeCharacteristicRequiredController
    : ControllerBase
{
    private const string ProductTypeNotFoundCode =
        "catalog.product_type.not_found";

    private const string
        ProductTypeCharacteristicNotFoundCode =
            "catalog.product_type_characteristic.not_found";

    private const string CannotBeRequiredCode =
        "catalog.product_type_characteristic.cannot_be_required";

    private readonly
        SetProductTypeCharacteristicRequiredCommandHandler
        _handler;

    public SetProductTypeCharacteristicRequiredController(
        SetProductTypeCharacteristicRequiredCommandHandler
            handler)
    {
        _handler = handler;
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SetRequired(
        string productTypeCode,
        Guid characteristicDefinitionId,
        [FromBody]
        SetProductTypeCharacteristicRequiredRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command =
            new SetProductTypeCharacteristicRequiredCommand(
                productTypeCode,
                characteristicDefinitionId,
                request.IsRequired);

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
                return NotFound(result.Error.Message);
            }

            if (string.Equals(
                    result.Error.Code,
                    CannotBeRequiredCode,
                    StringComparison.Ordinal))
            {
                return Conflict(result.Error.Message);
            }

            return BadRequest(result.Error.Message);
        }

        return NoContent();
    }
}