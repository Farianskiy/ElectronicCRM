using ElectronicService.Contracts.Catalog.Recognition.Management;
using ElectronicService.Core.Catalog.Recognition.Management.UpdateProfile;
using ElectronicService.Web.Controllers.Catalog.Recognition.Profiles.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Recognition.Profiles.UpdateProfile;

[Authorize(Roles = "Technical")]
[ApiController]
[Route("api/catalog/recognition/profiles/{profileId:guid}")]
public sealed class UpdateCatalogCharacteristicRecognitionProfileController : ControllerBase
{
    private readonly UpdateCatalogCharacteristicRecognitionProfileCommandHandler _handler;

    public UpdateCatalogCharacteristicRecognitionProfileController(
        UpdateCatalogCharacteristicRecognitionProfileCommandHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _handler = handler;
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateProfile(
        Guid profileId,
        [FromBody] UpdateCatalogCharacteristicRecognitionProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new UpdateCatalogCharacteristicRecognitionProfileCommand(
            profileId,
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

        return NoContent();
    }
}