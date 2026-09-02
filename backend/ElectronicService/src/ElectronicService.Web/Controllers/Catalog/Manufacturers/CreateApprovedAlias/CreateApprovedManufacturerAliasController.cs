using ElectronicService.Contracts.Catalog.Manufacturers;
using ElectronicService.Core.Catalog.Manufacturers.CreateApprovedAlias;
using ElectronicService.Web.Controllers.Catalog.Manufacturers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Manufacturers.CreateApprovedAlias;

[Authorize(Roles = "Technical")]
[ApiController]
[Route("api/catalog/manufacturer-aliases")]
public sealed class CreateApprovedManufacturerAliasController : ControllerBase
{
    private readonly CreateApprovedManufacturerAliasCommandHandler _handler;

    public CreateApprovedManufacturerAliasController(CreateApprovedManufacturerAliasCommandHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _handler = handler;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateApprovedManufacturerAliasResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateApprovedManufacturerAliasResponse>> CreateApprovedAlias(
        [FromBody] CreateApprovedManufacturerAliasRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new CreateApprovedManufacturerAliasCommand(
            request.ManufacturerId,
            request.Phrase);

        var result = await _handler.Handle(command, cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
        {
            return this.ToCatalogManufacturerProblem(result.Error);
        }

        var response = new CreateApprovedManufacturerAliasResponse(
            result.Value.ManufacturerAliasId,
            result.Value.ManufacturerId,
            result.Value.ManufacturerName,
            result.Value.Phrase,
            result.Value.NormalizedPhrase,
            result.Value.Status,
            result.Value.Source);

        return Created(
            new Uri($"/api/catalog/manufacturer-aliases/{result.Value.ManufacturerAliasId}", UriKind.Relative),
            response);
    }
}