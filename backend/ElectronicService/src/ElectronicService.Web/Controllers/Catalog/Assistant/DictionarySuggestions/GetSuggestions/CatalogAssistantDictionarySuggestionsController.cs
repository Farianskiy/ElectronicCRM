using ElectronicService.Contracts.Catalog.Assistant.DictionarySuggestions;
using ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.GetSuggestions;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Assistant.DictionarySuggestions.GetSuggestions;

[ApiController]
[Route("api/catalog/assistant/dictionary-suggestions")]
public sealed class CatalogAssistantDictionarySuggestionsController : ControllerBase
{
    private readonly GetCatalogAssistantDictionarySuggestionsQueryHandler _handler;

    public CatalogAssistantDictionarySuggestionsController(GetCatalogAssistantDictionarySuggestionsQueryHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _handler = handler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(CatalogAssistantDictionarySuggestionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CatalogAssistantDictionarySuggestionsResponse>> GetSuggestions(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetCatalogAssistantDictionarySuggestionsQuery(
            status,
            page,
            pageSize);

        var result = await _handler.Handle(query, cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
        {
            return BadRequest(result.Error.Message);
        }

        var items = result.Value.Items.Select(suggestion => new CatalogAssistantDictionarySuggestionResponse(
            suggestion.Id,
            suggestion.OriginalMessage,
            suggestion.UnknownPhrase,
            suggestion.NormalizedUnknownPhrase,
            suggestion.SuggestedKind,
            suggestion.SuggestedTargetCode,
            suggestion.SuggestedTargetValue,
            suggestion.Confidence,
            suggestion.Source,
            suggestion.ProductTypeId,
            suggestion.ProductTypeCode,
            suggestion.ProductTypeName,
            suggestion.CharacteristicDefinitionId,
            suggestion.CharacteristicCode,
            suggestion.CharacteristicName,
            suggestion.OccurrenceCount,
            suggestion.AcceptedEvidenceCount,
            suggestion.CorrectedEvidenceCount,
            suggestion.RejectedEvidenceCount,
            suggestion.GeneratedAutomatically,
            suggestion.EvidenceExamples.Select(evidence => new CatalogAssistantDictionarySuggestionEvidenceExampleResponse(
                evidence.FeedbackId,
                evidence.ProductName,
                evidence.FeedbackType,
                evidence.SuggestedRawValue,
                evidence.SuggestedNormalizedValue,
                evidence.FinalNormalizedValue,
                evidence.SuggestedConfidence,
                evidence.SuggestedSource,
                evidence.SpanStart,
                evidence.SpanLength,
                evidence.LabelQuality,
                evidence.FinalizedAtUtc)).ToList(),
            suggestion.ApprovedPhrase,
            suggestion.ApprovedKind,
            suggestion.ApprovedTargetCode,
            suggestion.ApprovedTargetValue,
            suggestion.ApprovedProductTypeId,
            suggestion.ApprovedProductTypeCode,
            suggestion.ApprovedProductTypeName,
            suggestion.ApprovedCharacteristicDefinitionId,
            suggestion.ApprovedCharacteristicCode,
            suggestion.ApprovedCharacteristicName,
            suggestion.ApprovedPriority,
            suggestion.CreatedDictionaryTermId,
            suggestion.Status,
            suggestion.CreatedByUserId,
            suggestion.CreatedAtUtc,
            suggestion.ReviewedByUserId,
            suggestion.ReviewedAtUtc,
            suggestion.ReviewComment)).ToList();

        return Ok(new CatalogAssistantDictionarySuggestionsResponse(
            items,
            result.Value.Page,
            result.Value.PageSize,
            result.Value.TotalCount));
    }
}