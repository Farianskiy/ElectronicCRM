using ElectronicService.Contracts.Catalog.Manufacturers;
using ElectronicService.Core.Catalog.Manufacturers.MarkPhraseAsNoise;
using ElectronicService.Web.Controllers.Catalog.Manufacturers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Manufacturers.MarkPhraseAsNoise;

[Authorize(Roles = "Technical")]
[ApiController]
[Route("api/catalog/manufacturer-noise-phrases")]
public sealed class MarkManufacturerPhraseAsNoiseController : ControllerBase
{
    private readonly MarkManufacturerPhraseAsNoiseCommandHandler _handler;

    public MarkManufacturerPhraseAsNoiseController(MarkManufacturerPhraseAsNoiseCommandHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _handler = handler;
    }

    [HttpPost]
    [ProducesResponseType(typeof(MarkManufacturerPhraseAsNoiseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MarkManufacturerPhraseAsNoiseResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MarkManufacturerPhraseAsNoiseResponse>> MarkAsNoise(
        [FromBody] MarkManufacturerPhraseAsNoiseRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new MarkManufacturerPhraseAsNoiseCommand(
            request.Phrase,
            request.Reason);

        var result = await _handler.Handle(command, cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
        {
            return this.ToCatalogManufacturerProblem(result.Error);
        }

        var response = new MarkManufacturerPhraseAsNoiseResponse(
            result.Value.ManufacturerNoisePhraseId,
            result.Value.Phrase,
            result.Value.NormalizedPhrase,
            result.Value.Reason,
            result.Value.IsActive,
            result.Value.CreatedByUserId,
            result.Value.UpdatedByUserId,
            result.Value.CreatedAtUtc,
            result.Value.UpdatedAtUtc,
            result.Value.DeactivatedAtUtc,
            result.Value.Action.ToString());

        if (result.Value.Action == MarkManufacturerPhraseAsNoiseAction.Created)
        {
            return Created(
                new Uri($"/api/catalog/manufacturer-noise-phrases/{result.Value.ManufacturerNoisePhraseId}", UriKind.Relative),
                response);
        }

        return Ok(response);
    }
}