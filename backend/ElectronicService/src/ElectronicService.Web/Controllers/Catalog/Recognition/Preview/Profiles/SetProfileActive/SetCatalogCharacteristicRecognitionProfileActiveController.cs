using ElectronicService.Contracts.Catalog.Recognition.Management;
using ElectronicService.Core.Catalog.Recognition.Management.SetProfileActive;
using ElectronicService.Web.Controllers.Catalog.Recognition.Profiles.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Recognition.Profiles.SetProfileActive;

[Authorize(Roles = "Technical")]
[ApiController]
[Route("api/catalog/recognition/profiles/{profileId:guid}/active")]
public sealed class SetCatalogCharacteristicRecognitionProfileActiveController : ControllerBase
{
    private readonly SetCatalogCharacteristicRecognitionProfileActiveCommandHandler _handler;

    public SetCatalogCharacteristicRecognitionProfileActiveController(
        SetCatalogCharacteristicRecognitionProfileActiveCommandHandler handler)
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
    public async Task<IActionResult> SetProfileActive(
        Guid profileId,
        [FromBody] SetCatalogCharacteristicRecognitionProfileActiveRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new SetCatalogCharacteristicRecognitionProfileActiveCommand(
            profileId,
            request.IsActive);

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