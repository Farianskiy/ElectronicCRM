using ElectronicService.Contracts.Catalog
    .CharacteristicDefinitions;
using ElectronicService.Core.Catalog
    .CharacteristicDefinitions.CreateDefinition;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog
    .CharacteristicDefinitions.CreateDefinition;

[Authorize(Roles = "Technical")]
[ApiController]
[Route("api/catalog/characteristic-definitions")]
public sealed class
    CreateCharacteristicDefinitionController
    : ControllerBase
{
    private const string AlreadyExistsCode =
        "catalog.characteristic_already_exists";

    private readonly
        CreateCharacteristicDefinitionCommandHandler
        _handler;

    public CreateCharacteristicDefinitionController(
        CreateCharacteristicDefinitionCommandHandler
            handler)
    {
        _handler = handler;
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(CreateCharacteristicDefinitionResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<
        CreateCharacteristicDefinitionResponse>>
        CreateDefinition(
            [FromBody]
            CreateCharacteristicDefinitionRequest request,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command =
            new CreateCharacteristicDefinitionCommand(
                request.Code,
                request.Name,
                request.DataType,
                request.Unit);

        var result = await _handler
            .Handle(command, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            if (string.Equals(
                    result.Error.Code,
                    AlreadyExistsCode,
                    StringComparison.Ordinal))
            {
                return Conflict(
                    result.Error.Message);
            }

            return BadRequest(
                result.Error.Message);
        }

        var response =
            new CreateCharacteristicDefinitionResponse(
                result.Value);

        return StatusCode(
            StatusCodes.Status201Created,
            response);
    }
}