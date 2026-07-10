using ElectronicService.Contracts.Catalog.Assistant.DictionarySuggestions;
using ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.RejectSuggestion;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Assistant.DictionarySuggestions.RejectSuggestion;

[ApiController]
[Route("api/catalog/assistant/dictionary-suggestions")]
public sealed class CatalogAssistantDictionarySuggestionRejectionController : ControllerBase
{
    private readonly RejectCatalogAssistantDictionarySuggestionCommandHandler _handler;

    public CatalogAssistantDictionarySuggestionRejectionController(
        RejectCatalogAssistantDictionarySuggestionCommandHandler handler)
    {
        _handler = handler;
    }

    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Reject(
        Guid id,
        [FromBody] ReviewCatalogAssistantDictionarySuggestionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new RejectCatalogAssistantDictionarySuggestionCommand(
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