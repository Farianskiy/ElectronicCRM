using ElectronicService.Contracts.Catalog.Products.Management;
using ElectronicService.Core.Catalog.Products.UpdateGeneralInformation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Products
    .UpdateGeneralInformation;

[Authorize(Roles = "Technical")]
[ApiController]
[Route("api/catalog/products/{id:guid}/general-information")]
public sealed class CatalogProductGeneralInformationController
    : ControllerBase
{
    private const string ProductNotFoundCode =
        "catalog.product.not_found";

    private const string ManufacturerNotFoundCode =
        "catalog.manufacturer.not_found";

    private readonly
        UpdateProductGeneralInformationCommandHandler _handler;

    public CatalogProductGeneralInformationController(
        UpdateProductGeneralInformationCommandHandler handler)
    {
        _handler = handler;
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateGeneralInformation(
        Guid id,
        [FromBody] UpdateProductGeneralInformationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new UpdateProductGeneralInformationCommand(
            id,
            request.Name,
            request.Article,
            request.ManufacturerId);

        var result = await _handler
            .Handle(command, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            if (string.Equals(
                    result.Error.Code,
                    ProductNotFoundCode,
                    StringComparison.Ordinal)
                || string.Equals(
                    result.Error.Code,
                    ManufacturerNotFoundCode,
                    StringComparison.Ordinal))
            {
                return NotFound(result.Error.Message);
            }

            return BadRequest(result.Error.Message);
        }

        return NoContent();
    }
}