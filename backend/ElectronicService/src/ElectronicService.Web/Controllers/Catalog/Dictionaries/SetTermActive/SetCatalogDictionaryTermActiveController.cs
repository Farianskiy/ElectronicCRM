using ElectronicService.Contracts.Catalog.Dictionaries;
using ElectronicService.Core.Catalog.Dictionaries.SetTermActive;
using ElectronicService.Web.Controllers.Catalog.Dictionaries.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Dictionaries.SetTermActive;

[Authorize(Roles = "Technical")]
[ApiController]
[Route("api/catalog/dictionary/terms/{termId:guid}/active")]
public sealed class SetCatalogDictionaryTermActiveController : ControllerBase
{
    private readonly SetCatalogDictionaryTermActiveCommandHandler _handler;

    public SetCatalogDictionaryTermActiveController(SetCatalogDictionaryTermActiveCommandHandler handler)
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
    public async Task<IActionResult> SetActive(
        Guid termId,
        [FromBody] SetCatalogDictionaryTermActiveRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new SetCatalogDictionaryTermActiveCommand(termId, request.IsActive, request.Reason);

        var result = await _handler.Handle(command, cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
        {
            return this.ToCatalogDictionaryProblem(result.Error);
        }

        return NoContent();
    }
}