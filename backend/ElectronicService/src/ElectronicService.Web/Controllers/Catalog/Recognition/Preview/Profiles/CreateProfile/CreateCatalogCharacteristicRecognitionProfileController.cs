using ElectronicService.Contracts.Catalog.Recognition.Management;
using ElectronicService.Core.Catalog.Recognition.Management.CreateProfile;
using ElectronicService.Web.Controllers.Catalog.Recognition.Profiles.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Recognition.Profiles.CreateProfile;

[Authorize(Roles = "Technical")]
[ApiController]
[Route("api/catalog/recognition/profiles")]
public sealed class CreateCatalogCharacteristicRecognitionProfileController : ControllerBase
{
    private readonly CreateCatalogCharacteristicRecognitionProfileCommandHandler _handler;

    public CreateCatalogCharacteristicRecognitionProfileController(
        CreateCatalogCharacteristicRecognitionProfileCommandHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _handler = handler;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateCatalogCharacteristicRecognitionProfileResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateCatalogCharacteristicRecognitionProfileResponse>> CreateProfile(
        [FromBody] CreateCatalogCharacteristicRecognitionProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new CreateCatalogCharacteristicRecognitionProfileCommand(
            request.ProductTypeCode,
            request.CharacteristicDefinitionId,
            request.StrategyKind,
            request.Priority,
            request.MinimumConfidence,
            request.ConfigurationJson);

        var result = await _handler
            .Handle(command, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return this.ToCatalogRecognitionProfileProblem(result.Error);
        }

        var response = new CreateCatalogCharacteristicRecognitionProfileResponse(
            result.Value.ProfileId);

        return StatusCode(StatusCodes.Status201Created, response);
    }
}