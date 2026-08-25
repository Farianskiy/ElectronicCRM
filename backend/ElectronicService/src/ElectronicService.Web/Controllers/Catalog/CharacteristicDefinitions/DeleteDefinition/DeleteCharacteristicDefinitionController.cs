using ElectronicService.Core.Catalog.CharacteristicDefinitions.DeleteDefinition;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.CharacteristicDefinitions.DeleteDefinition;

[Authorize(Roles = "Technical")]
[ApiController]
[Route("api/catalog/characteristic-definitions/{characteristicDefinitionId:guid}")]
public sealed class DeleteCharacteristicDefinitionController : ControllerBase
{
    private const string NotFoundCode = "catalog.characteristic_definition.not_found";
    private const string CannotBeDeletedCode = "catalog.characteristic_definition.cannot_be_deleted";

    private readonly DeleteCharacteristicDefinitionCommandHandler _handler;

    public DeleteCharacteristicDefinitionController(DeleteCharacteristicDefinitionCommandHandler handler)
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
    public async Task<IActionResult> DeleteDefinition(
        Guid characteristicDefinitionId,
        CancellationToken cancellationToken = default)
    {
        var command = new DeleteCharacteristicDefinitionCommand(characteristicDefinitionId);

        var result = await _handler
            .Handle(command, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            if (string.Equals(result.Error.Code, NotFoundCode, StringComparison.Ordinal))
            {
                return NotFound(result.Error.Message);
            }

            if (string.Equals(result.Error.Code, CannotBeDeletedCode, StringComparison.Ordinal))
            {
                return Conflict(result.Error.Message);
            }

            return BadRequest(result.Error.Message);
        }

        return NoContent();
    }
}