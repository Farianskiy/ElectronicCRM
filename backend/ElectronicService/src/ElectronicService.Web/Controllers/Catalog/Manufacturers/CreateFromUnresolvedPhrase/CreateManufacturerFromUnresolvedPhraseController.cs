using ElectronicService.Contracts.Catalog.Manufacturers;
using ElectronicService.Core.Catalog.Manufacturers.CreateFromUnresolvedPhrase;
using ElectronicService.Web.Controllers.Catalog.Manufacturers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Manufacturers.CreateFromUnresolvedPhrase;

[Authorize(Roles = "Technical")]
[ApiController]
[Route("api/catalog/manufacturers")]
public sealed class CreateManufacturerFromUnresolvedPhraseController : ControllerBase
{
    private readonly CreateManufacturerFromUnresolvedPhraseCommandHandler _handler;

    public CreateManufacturerFromUnresolvedPhraseController(CreateManufacturerFromUnresolvedPhraseCommandHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _handler = handler;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateManufacturerFromUnresolvedPhraseResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateManufacturerFromUnresolvedPhraseResponse>> CreateManufacturer(
        [FromBody] CreateManufacturerFromUnresolvedPhraseRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new CreateManufacturerFromUnresolvedPhraseCommand(
            request.CanonicalName,
            request.SourcePhrase);

        var result = await _handler.Handle(command, cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
        {
            return this.ToCatalogManufacturerProblem(result.Error);
        }

        var response = new CreateManufacturerFromUnresolvedPhraseResponse(
            result.Value.ManufacturerId,
            result.Value.ManufacturerName,
            result.Value.NormalizedManufacturerName,
            result.Value.ManufacturerAliasId,
            result.Value.AliasPhrase,
            result.Value.NormalizedAliasPhrase,
            result.Value.AliasStatus,
            result.Value.AliasSource,
            result.Value.ResolutionSource);

        return Created(
            new Uri($"/api/catalog/manufacturers/{result.Value.ManufacturerId}", UriKind.Relative),
            response);
    }
}