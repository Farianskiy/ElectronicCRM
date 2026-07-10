using ElectronicService.Contracts.Catalog.Assistant.DictionarySuggestions;
using ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.CreateSuggestion;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Assistant.DictionarySuggestions.CreateSuggestion;

[ApiController]
[Route("api/catalog/assistant/dictionary-suggestions")]
public sealed class CatalogAssistantDictionarySuggestionCreationController : ControllerBase
{
    private readonly CreateCatalogAssistantDictionarySuggestionCommandHandler _handler;

    public CatalogAssistantDictionarySuggestionCreationController(
        CreateCatalogAssistantDictionarySuggestionCommandHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateCatalogAssistantDictionarySuggestionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateCatalogAssistantDictionarySuggestionResponse>> Create(
        [FromBody] CreateCatalogAssistantDictionarySuggestionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new CreateCatalogAssistantDictionarySuggestionCommand(
            request.OriginalMessage,
            request.UnknownPhrase,
            request.SuggestedKind,
            request.SuggestedTargetCode,
            request.SuggestedTargetValue,
            request.Confidence,
            request.CreatedByUserId);

        var result = await _handler
            .Handle(command, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return BadRequest(result.Error.Message);
        }

        var response = new CreateCatalogAssistantDictionarySuggestionResponse(
            result.Value.Id,
            result.Value.Status,
            "Предложение отправлено техническому специалисту.");

        return Created(
            new Uri($"/api/catalog/assistant/dictionary-suggestions/{result.Value.Id}", UriKind.Relative),
            response);
    }
}