using ElectronicService.Contracts.Catalog.ImportBatches;
using ElectronicService.Core.Catalog.ImportBatches.GetRowExplanations;
using ElectronicService.Core.Catalog.ProductNames.Explanation;
using ElectronicService.Web.Auth;
using ElectronicService.Web.Controllers.Catalog.ImportBatches.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.ImportBatches.GetRowExplanations;

[ApiController]
[Authorize(Roles = "Regular,Manager,Technical")]
[Route("api/catalog/import-batches")]
public sealed class GetCatalogImportRowExplanationsController : ControllerBase
{
    [HttpGet("{batchId:guid}/rows/name-explanations")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(
        typeof(GetCatalogImportRowExplanationsResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<
        ActionResult<GetCatalogImportRowExplanationsResponse>> Get(
        Guid batchId,
        [FromQuery] Guid[] rowIds,
        [FromQuery] uint? expectedVersion,
        [FromServices] GetCatalogImportRowExplanationsQueryHandler handler,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
            return this.ToCurrentUserProblem();

        var result = await handler.Handle(
            new GetCatalogImportRowExplanationsQuery(
                batchId,
                userId,
                rowIds,
                expectedVersion),
            cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
        {
            return this.ToCatalogImportProblem(
                result.Error,
                "Не удалось получить объяснения наименований.");
        }

        return Ok(new GetCatalogImportRowExplanationsResponse(
            result.Value.BatchId,
            result.Value.BatchVersion,
            result.Value.GeneratedAtUtc,
            result.Value.Items
                .Select(item => new CatalogImportRowExplanationResponse(
                    item.RowId,
                    item.RowNumber,
                    item.ProductName,
                    item.Status.ToString(),
                    item.HasConflicts,
                    MapExplanation(item)))
                .ToArray()));
    }

    private static CatalogImportProductNameExplanationSampleResponse?
        MapExplanation(CatalogImportRowExplanationResult row)
    {
        var explanation = row.Explanation;

        if (explanation is null)
            return null;

        string kind;

        if (explanation.IsFullyExplained)
        {
            kind = "FullyExplained";
        }
        else if (explanation.Evidence.Count > 0)
        {
            kind = "PartiallyExplained";
        }
        else
        {
            kind = "Unexplained";
        }

        return new CatalogImportProductNameExplanationSampleResponse(
            row.RowNumber,
            kind,
            explanation.ProductName,
            explanation.MeaningfulCharactersCount,
            explanation.CoveredMeaningfulCharactersCount,
            explanation.Coverage,
            explanation.IsFullyExplained,
            explanation.HasUnexplainedSpans,
            explanation.Evidence.Count(span =>
                span.Kind == CatalogProductNameEvidenceKind.Manufacturer),
            explanation.Evidence.Count(span =>
                span.Kind == CatalogProductNameEvidenceKind.ProductType),
            explanation.Evidence.Count(span =>
                span.Kind == CatalogProductNameEvidenceKind.Characteristic),
            explanation.Evidence
                .Select(span => new CatalogImportProductNameEvidenceSpanResponse(
                    span.Kind.ToString(),
                    span.TargetCode,
                    span.TargetValue,
                    span.RawValue,
                    span.Source,
                    span.Confidence,
                    span.Priority,
                    span.StartIndex,
                    span.Length,
                    span.EndIndex))
                .ToArray(),
            explanation.UnexplainedSpans
                .Select(span =>
                    new CatalogImportProductNameUnexplainedSpanResponse(
                        span.RawValue,
                        span.StartIndex,
                        span.Length,
                        span.EndIndex))
                .ToArray());
    }
}