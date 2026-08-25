using ElectronicService.Contracts.Catalog.ImportBatches;
using ElectronicService.Core.Catalog.ImportBatches.AnalyzeCatalogImportBatch;
using ElectronicService.Web.Auth;
using ElectronicService.Web.Controllers.Catalog.ImportBatches.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.ImportBatches.AnalyzeCatalogImportBatch;

[ApiController]
[Authorize(Roles = "Regular,Manager,Technical")]
[Route("api/catalog/import-batches")]
public sealed class AnalyzeCatalogImportBatchController : ControllerBase
{
    private const string ProblemTitle =
        "Не удалось проанализировать Excel-файл.";

    [HttpPost("{batchId:guid}/analyze")]
    [ProducesResponseType(
        typeof(AnalyzeCatalogImportBatchResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AnalyzeCatalogImportBatchResponse>> Analyze(
        Guid batchId,
        [FromQuery] Guid? productTypeId,
        [FromServices] AnalyzeCatalogImportBatchCommandHandler handler,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var currentUserId))
        {
            return this.ToCurrentUserProblem();
        }

        var command =
            new AnalyzeCatalogImportBatchCommand(
                batchId,
                currentUserId,
                productTypeId);

        var result = await handler
            .Handle(
                command,
                cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return this.ToCatalogImportProblem(
                result.Error,
                ProblemTitle);
        }

        CatalogImportRecognitionShadowResponse? recognitionShadow = null;

        if (result.Value.RecognitionShadow is not null)
        {
            var shadow = result.Value.RecognitionShadow;

            recognitionShadow =
                new CatalogImportRecognitionShadowResponse(
                    shadow.RowsAnalyzed,
                    shadow.RowsWithRecognition,
                    shadow.FailedRowsCount,
                    shadow.ExplicitValuesCount,
                    shadow.RecognizedValuesCount,
                    shadow.MatchesCount,
                    shadow.ConflictsCount,
                    shadow.NotRecognizedCount,
                    shadow.RecognitionWithoutExplicitValueCount,
                    shadow.AmbiguousCount,
                    shadow.Characteristics
                        .Select(item =>
                            new CatalogImportRecognitionShadowCharacteristicResponse(
                                item.CharacteristicCode,
                                item.CharacteristicName,
                                item.ExplicitValuesCount,
                                item.RecognizedValuesCount,
                                item.MatchesCount,
                                item.ConflictsCount,
                                item.NotRecognizedCount,
                                item.RecognitionWithoutExplicitValueCount,
                                item.AmbiguousCount))
                        .ToArray(),
                    shadow.ConflictGroups
                        .Select(item =>
                            new CatalogImportRecognitionShadowConflictGroupResponse(
                                item.CharacteristicCode,
                                item.CharacteristicName,
                                item.ExcelValue,
                                item.RecognizedValue,
                                item.RawRecognizedValue,
                                item.RecognitionSource.ToString(),
                                item.Confidence,
                                item.SpanStart,
                                item.SpanLength,
                                item.Priority,
                                item.RecognizerKey,
                                item.OccurrenceCount,
                                item.ExampleRowNumbers,
                                item.ExampleProductNames))
                        .ToArray(),
                    shadow.Samples
                        .Select(item =>
                            new CatalogImportRecognitionShadowSampleResponse(
                                item.RowNumber,
                                item.Kind.ToString(),
                                item.CharacteristicCode,
                                item.CharacteristicName,
                                item.ProductName,
                                item.ExcelValue,
                                item.RecognizedValue,
                                item.RawRecognizedValue,
                                item.Confidence,
                                item.RecognitionSource,
                                item.RecognizerKey,
                                item.SpanStart,
                                item.SpanLength,
                                item.Priority,
                                item.Details))
                        .ToArray());
        }

        var manufacturerResolutionSummary = new CatalogImportManufacturerResolutionSummaryResponse(
            result.Value.ManufacturerResolutionSummary.RowsWithManufacturerValueCount,
            result.Value.ManufacturerResolutionSummary.ResolvedByExactNameRowsCount,
            result.Value.ManufacturerResolutionSummary.ResolvedByApprovedAliasRowsCount,
            result.Value.ManufacturerResolutionSummary.IgnoredNoiseRowsCount,
            result.Value.ManufacturerResolutionSummary.UnresolvedRowsCount,
            result.Value.ManufacturerResolutionSummary.Groups
                .Select(group =>
                    new CatalogImportManufacturerResolutionGroupResponse(
                        group.SourceValue,
                        group.NormalizedSourceValue,
                        group.Status.ToString(),
                        group.ManufacturerId,
                        group.ResolvedManufacturerName,
                        group.Source.ToString(),
                        group.ManufacturerAliasId,
                        group.ManufacturerNoisePhraseId,
                        group.NoiseReason,
                        group.OccurrenceCount,
                        group.ExampleRowNumbers,
                        group.ExampleProductNames))
                .ToArray());

        var recognitionEnrichment = new CatalogImportRecognitionEnrichmentResponse(
            result.Value.RecognitionEnrichment.RowsAnalyzedCount,
            result.Value.RecognitionEnrichment.FilledRowsCount,
            result.Value.RecognitionEnrichment.FilledValuesCount,
            result.Value.RecognitionEnrichment.BlockedByRecognitionConflictCount,
            result.Value.RecognitionEnrichment.BlockedByLowConfidenceCount,
            result.Value.RecognitionEnrichment.BlockedByUnsupportedSourceCount,
            result.Value.RecognitionEnrichment.BlockedByInvalidExcelValueCount,
            result.Value.RecognitionEnrichment.BlockedByInvalidRecognizedValueCount,
            result.Value.RecognitionEnrichment.FailedRecognitionRowsCount,
            result.Value.RecognitionEnrichment.AppliedValuesDetailsTruncated,
            result.Value.RecognitionEnrichment.AppliedValues
                .Select(value => new CatalogImportRecognitionAppliedValueResponse(
                    value.RowNumber,
                    value.CharacteristicDefinitionId,
                    value.CharacteristicCode,
                    value.CharacteristicName,
                    value.Value,
                    value.RawValue,
                    value.RecognitionSource.ToString(),
                    value.Confidence,
                    value.SpanStart,
                    value.SpanLength,
                    value.Priority,
                    value.RecognizerKey))
                .ToArray());

        return Ok(
            new AnalyzeCatalogImportBatchResponse(
                result.Value.BatchId,
                result.Value.Status.ToString(),
                result.Value.ProductTypeId,
                result.Value.ColumnsCount,
                result.Value.UnmappedColumnsCount,
                result.Value.UnconfirmedColumnsCount,
                result.Value.RowsCount,
                result.Value.ValidRowsCount,
                result.Value.ErrorRowsCount,
                manufacturerResolutionSummary,
                recognitionShadow,
                recognitionEnrichment));
    }
}