using ElectronicService.Contracts.Catalog.Assistant.DictionarySuggestions;
using ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.ApproveSuggestion;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Assistant.DictionarySuggestions.ApproveSuggestion;

[ApiController]
[Route("api/catalog/assistant/dictionary-suggestions")]
public sealed class CatalogAssistantDictionarySuggestionApprovalController : ControllerBase
{
    private readonly ApproveCatalogAssistantDictionarySuggestionCommandHandler _handler;

    public CatalogAssistantDictionarySuggestionApprovalController(
        ApproveCatalogAssistantDictionarySuggestionCommandHandler handler)
    {
        _handler = handler;
    }

    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Approve(
        Guid id,
        [FromBody] ReviewCatalogAssistantDictionarySuggestionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new ApproveCatalogAssistantDictionarySuggestionCommand(
            id,
            request.ReviewedByUserId,
            request.ReviewComment);

        var result = await _handler
            .Handle(command, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return BadRequest(result.Error.Message);
        }

        return NoContent();
    }
}