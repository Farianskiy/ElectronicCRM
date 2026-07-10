using ElectronicService.Contracts.Catalog.Assistant.DictionarySuggestions;
using ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.GetSuggestions;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Assistant.DictionarySuggestions.GetSuggestions;

[ApiController]
[Route("api/catalog/assistant/dictionary-suggestions")]
public sealed class CatalogAssistantDictionarySuggestionsController : ControllerBase
{
    private readonly GetCatalogAssistantDictionarySuggestionsQueryHandler _handler;

    public CatalogAssistantDictionarySuggestionsController(
        GetCatalogAssistantDictionarySuggestionsQueryHandler handler)
    {
        _handler = handler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(CatalogAssistantDictionarySuggestionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CatalogAssistantDictionarySuggestionsResponse>> GetSuggestions(
        [FromQuery] Guid technicalUserId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetCatalogAssistantDictionarySuggestionsQuery(
            technicalUserId,
            status,
            page,
            pageSize);

        var result = await _handler
            .Handle(query, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return BadRequest(result.Error.Message);
        }

        return Ok(new CatalogAssistantDictionarySuggestionsResponse(
            result.Value.Items.Select(suggestion => new CatalogAssistantDictionarySuggestionResponse(
                suggestion.Id,
                suggestion.OriginalMessage,
                suggestion.UnknownPhrase,
                suggestion.NormalizedUnknownPhrase,
                suggestion.SuggestedKind,
                suggestion.SuggestedTargetCode,
                suggestion.SuggestedTargetValue,
                suggestion.Confidence,
                suggestion.Status,
                suggestion.CreatedByUserId,
                suggestion.CreatedAtUtc,
                suggestion.ReviewedByUserId,
                suggestion.ReviewedAtUtc,
                suggestion.ReviewComment)).ToList(),
            result.Value.Page,
            result.Value.PageSize,
            result.Value.TotalCount));
    }
}