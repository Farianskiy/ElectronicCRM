using ElectronicService.Contracts.Catalog.Assistant.DictionarySuggestions;
using ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.ApproveSuggestion;
using ElectronicService.Web.Controllers.Catalog.Dictionaries.Common;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Assistant.DictionarySuggestions.ApproveSuggestion;

[ApiController]
[Route("api/catalog/assistant/dictionary-suggestions")]
public sealed class CatalogAssistantDictionarySuggestionApprovalController : ControllerBase
{
    private readonly ApproveCatalogAssistantDictionarySuggestionCommandHandler _handler;

    public CatalogAssistantDictionarySuggestionApprovalController(ApproveCatalogAssistantDictionarySuggestionCommandHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _handler = handler;
    }

    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Approve(
        Guid id,
        [FromBody] ApproveCatalogAssistantDictionarySuggestionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new ApproveCatalogAssistantDictionarySuggestionCommand(
            id,
            request.Phrase,
            request.Kind,
            request.TargetCode,
            request.TargetValue,
            request.ProductTypeCode,
            request.Priority,
            request.ReviewComment);

        var result = await _handler.Handle(command, cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
        {
            return this.ToCatalogDictionaryProblem(result.Error);
        }

        return NoContent();
    }
}