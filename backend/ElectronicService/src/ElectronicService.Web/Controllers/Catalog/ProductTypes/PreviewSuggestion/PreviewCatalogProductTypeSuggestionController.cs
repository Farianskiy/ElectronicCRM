using ElectronicService.Contracts.Catalog.ProductTypes.Suggestions;
using ElectronicService.Core.Catalog.ProductTypes.Suggestions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.ProductTypes.PreviewSuggestion;

[Authorize(Roles = "Technical")]
[ApiController]
[Route("api/catalog/product-types/suggestions/preview")]
public sealed class PreviewCatalogProductTypeSuggestionController
    : ControllerBase
{
    private readonly PreviewCatalogProductTypeSuggestionQueryHandler _handler;

    public PreviewCatalogProductTypeSuggestionController(
        PreviewCatalogProductTypeSuggestionQueryHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _handler = handler;
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(CatalogProductTypeSuggestionPreviewResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CatalogProductTypeSuggestionPreviewResponse>> Preview(
        [FromBody] PreviewCatalogProductTypeSuggestionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ProductName))
        {
            return BadRequest("Наименование товара обязательно.");
        }

        var result = await _handler
            .Handle(
                new PreviewCatalogProductTypeSuggestionQuery(
                    request.ProductName),
                cancellationToken)
            .ConfigureAwait(false);

        var selectedCandidate = result.SelectedCandidate is null
            ? null
            : MapCandidate(result.SelectedCandidate);

        return Ok(
            new CatalogProductTypeSuggestionPreviewResponse(
                result.ProductName,
                result.NormalizedProductName,
                result.Status.ToString(),
                result.IsSuggested,
                result.IsConflict,
                selectedCandidate,
                result.Candidates
                    .Select(MapCandidate)
                    .ToArray()));
    }

    private static CatalogProductTypeSuggestionCandidateResponse MapCandidate(
        CatalogProductTypeSuggestionCandidate candidate)
    {
        return new CatalogProductTypeSuggestionCandidateResponse(
            candidate.ProductTypeId,
            candidate.ProductTypeCode,
            candidate.ProductTypeName,
            candidate.HighestPriority,
            candidate.Confidence,
            candidate.Evidence
                .Select(MapEvidence)
                .ToArray());
    }

    private static CatalogProductTypeSuggestionEvidenceResponse MapEvidence(
        CatalogProductTypeSuggestionEvidence evidence)
    {
        return new CatalogProductTypeSuggestionEvidenceResponse(
            evidence.DictionaryTermId,
            evidence.Phrase,
            evidence.RawValue,
            evidence.NormalizedValue,
            evidence.Priority,
            evidence.Source,
            evidence.StartIndex,
            evidence.Length,
            evidence.EndIndex);
    }
}