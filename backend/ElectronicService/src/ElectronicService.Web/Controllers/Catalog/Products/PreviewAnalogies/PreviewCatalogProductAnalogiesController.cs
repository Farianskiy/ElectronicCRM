using ElectronicService.Contracts.Catalog.Products.Analogies;
using ElectronicService.Core.Catalog.Products.Analogies;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.Products.PreviewAnalogies;

[PermissionAuthorize(UserPermissionCode.DictionariesManage)]
[ApiController]
[Route("api/catalog/products/analogies/preview")]
public sealed class PreviewCatalogProductAnalogiesController
    : ControllerBase
{
    private readonly PreviewCatalogProductAnalogiesQueryHandler _handler;
    private readonly CatalogImportAnalogyObservationService _observationService;

    public PreviewCatalogProductAnalogiesController(
        PreviewCatalogProductAnalogiesQueryHandler handler,
        CatalogImportAnalogyObservationService observationService)
    {
        _handler = handler;
        _observationService = observationService;
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(CatalogProductAnalogiesPreviewResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CatalogProductAnalogiesPreviewResponse>>
        Preview(
            [FromBody] PreviewCatalogProductAnalogiesRequest request,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ProductName)
            || request.ManufacturerId == Guid.Empty
            || request.ProductTypeId == Guid.Empty
            || request.ImportBatchId.HasValue != request.ImportRowId.HasValue
            || request.ImportBatchId == Guid.Empty
            || request.ImportRowId == Guid.Empty
            || request.MaximumResults is < 1 or > 100)
        {
            return BadRequest(
                "Укажите наименование, производителя и количество результатов от 1 до 100. Если тип товара передан, его идентификатор не должен быть пустым.");
        }

        var analogyRequest = new CatalogProductAnalogyRequest(
            request.ProductName,
            request.Article,
            request.ManufacturerId,
            request.ProductTypeId,
            request.KnownCharacteristics
                ?? new Dictionary<string, string>(StringComparer.Ordinal),
            MaximumResults: request.MaximumResults);

        var result = await _handler
            .Handle(
                analogyRequest,
                cancellationToken)
            .ConfigureAwait(false);

        if (request.ImportBatchId.HasValue && request.ImportRowId.HasValue)
        {
            var observation = await _observationService.RecordAsync(
                    request.ImportBatchId.Value,
                    request.ImportRowId.Value,
                    analogyRequest,
                    result,
                    cancellationToken)
                .ConfigureAwait(false);
            if (observation.IsFailure)
            {
                return BadRequest(observation.Error.Message);
            }

            result = observation.Value;
        }

        return Ok(
            new CatalogProductAnalogiesPreviewResponse(
                result.CandidateSnapshotId ?? string.Empty,
                result.CandidateSnapshotCreatedAtUtc ?? DateTime.UtcNow,
                result.EvaluatedCandidatesCount,
                result.Matches.Select(MapMatch).ToArray(),
                result.ExcludedCandidatesCount,
                result.ExclusionDiagnosticsTruncated,
                (result.ExcludedCandidates ?? [])
                    .Select(MapExclusion)
                    .ToArray(),
                result.CharacteristicProposals
                    .Select(MapCharacteristicProposal)
                    .ToArray(),
                result.ProductTypeProposals
                    .Select(MapProductTypeProposal)
                    .ToArray(),
                result.ConfigurationId,
                result.ConfigurationVersion,
                result.ConfigurationName,
                result.ConfigurationHash,
                result.IsSystemConfiguration));
    }

    private static CatalogProductAnalogyCandidateExclusionResponse MapExclusion(
        CatalogProductAnalogyCandidateExclusion exclusion)
    {
        return new CatalogProductAnalogyCandidateExclusionResponse(
            exclusion.ProductId,
            exclusion.Article,
            exclusion.Name,
            exclusion.ReasonCode,
            exclusion.Reason,
            exclusion.CandidateFamilyAnchors,
            exclusion.SharedNameTokens,
            exclusion.MatchingCharacteristicCount,
            exclusion.ConflictingCharacteristicCount);
    }

    private static CatalogProductAnalogyMatchResponse MapMatch(
        CatalogProductAnalogyMatch match)
    {
        return new CatalogProductAnalogyMatchResponse(
            match.ProductId,
            match.Article,
            match.Name,
            match.ProductTypeId,
            match.ProductTypeCode,
            match.ProductTypeName,
            match.MatchedName,
            match.ExactArticleMatch,
            match.ExactNameMatch,
            match.SharedNameTokenCount,
            match.QueryNameTokenCoverage,
            match.MatchingCharacteristicCount,
            match.ConflictingCharacteristicCount,
            match.SharedNameTokens,
            match.MatchingCharacteristicCodes,
            match.ConflictingCharacteristicCodes,
            match.Characteristics,
            match.MatchedFamilyAnchor,
            match.CandidateFamilyAnchors ?? []);
    }

    private static CatalogProductAnalogyCharacteristicProposalResponse
        MapCharacteristicProposal(
            CatalogProductAnalogyCharacteristicProposal proposal)
    {
        return new CatalogProductAnalogyCharacteristicProposalResponse(
            proposal.CharacteristicCode,
            proposal.ProposedValue,
            proposal.SupportingProductCount,
            proposal.ProductsWithCharacteristicCount,
            proposal.SupportRatio,
            proposal.IsSuggested,
            proposal.BlockingReasons,
            proposal.SupportingProductIds,
            proposal.AlternativeValues,
            proposal.FeedbackId,
            proposal.Mode.ToString(),
            proposal.IsAutoFillAllowed,
            proposal.AutoFillBlockingReason);
    }

    private static CatalogProductAnalogyProductTypeProposalResponse
        MapProductTypeProposal(
            CatalogProductAnalogyProductTypeProposal proposal)
    {
        return new CatalogProductAnalogyProductTypeProposalResponse(
            proposal.ProductTypeId,
            proposal.ProductTypeCode,
            proposal.ProductTypeName,
            proposal.SupportingProductCount,
            proposal.EligibleProductCount,
            proposal.SupportRatio,
            proposal.IsSuggested,
            proposal.BlockingReasons,
            proposal.SupportingProductIds);
    }
}
