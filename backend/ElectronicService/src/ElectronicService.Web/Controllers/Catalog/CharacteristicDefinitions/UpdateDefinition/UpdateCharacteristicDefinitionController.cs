using ElectronicService.Contracts.Catalog
    .CharacteristicDefinitions;
using ElectronicService.Core.Catalog
    .CharacteristicDefinitions.UpdateDefinition;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog
    .CharacteristicDefinitions.UpdateDefinition;

[Authorize(Roles = "Technical")]
[ApiController]
[Route(
    "api/catalog/characteristic-definitions/" +
    "{characteristicDefinitionId:guid}")]
public sealed class
    UpdateCharacteristicDefinitionController
    : ControllerBase
{
    private const string NotFoundCode =
        "catalog.characteristic_definition.not_found";

    private readonly
        UpdateCharacteristicDefinitionCommandHandler
        _handler;

    public UpdateCharacteristicDefinitionController(
        UpdateCharacteristicDefinitionCommandHandler
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
    public async Task<IActionResult> UpdateDefinition(
        Guid characteristicDefinitionId,
        [FromBody]
        UpdateCharacteristicDefinitionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command =
            new UpdateCharacteristicDefinitionCommand(
                characteristicDefinitionId,
                request.Name,
                request.Unit);

        var result = await _handler
            .Handle(command, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            if (string.Equals(
                    result.Error.Code,
                    NotFoundCode,
                    StringComparison.Ordinal))
            {
                return NotFound(
                    result.Error.Message);
            }

            return BadRequest(
                result.Error.Message);
        }

        return NoContent();
    }
}