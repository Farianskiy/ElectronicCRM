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

        var manufacturerRecognitionShadow =
    new CatalogImportManufacturerRecognitionShadowResponse(
        result.Value.ManufacturerRecognitionShadow.RowsAnalyzedCount,
        result.Value.ManufacturerRecognitionShadow.RowsWithExcelManufacturerValueCount,
        result.Value.ManufacturerRecognitionShadow.RowsWithResolvedExcelManufacturerCount,
        result.Value.ManufacturerRecognitionShadow.RowsWithRecognizedManufacturerCount,
        result.Value.ManufacturerRecognitionShadow.MatchesCount,
        result.Value.ManufacturerRecognitionShadow.ConflictsCount,
        result.Value.ManufacturerRecognitionShadow.SuggestionsCount,
        result.Value.ManufacturerRecognitionShadow.NameConflictsCount,
        result.Value.ManufacturerRecognitionShadow.NameUnresolvedCount,
        result.Value.ManufacturerRecognitionShadow.ComparisonUnavailableCount,
        result.Value.ManufacturerRecognitionShadow.SamplesTruncated,
        result.Value.ManufacturerRecognitionShadow.Samples
            .Select(sample =>
                new CatalogImportManufacturerRecognitionShadowSampleResponse(
                    sample.RowNumber,
                    sample.Kind.ToString(),
                    sample.ProductName,
                    sample.ExcelManufacturerId,
                    sample.ExcelManufacturerName,
                    sample.ExcelManufacturerResolutionSource,
                    sample.RecognizedManufacturerId,
                    sample.RecognizedManufacturerName,
                    sample.RawRecognizedValue,
                    sample.Confidence,
                    sample.RecognitionSource?.ToString(),
                    sample.SpanStart,
                    sample.SpanLength,
                    sample.Candidates
                        .Select(candidate =>
                            new CatalogImportManufacturerRecognitionShadowCandidateResponse(
                                candidate.ManufacturerId,
                                candidate.ManufacturerName,
                                candidate.RawValue,
                                candidate.NormalizedValue,
                                candidate.Confidence,
                                candidate.Source.ToString(),
                                candidate.ManufacturerAliasId,
                                candidate.SpanStart,
                                candidate.SpanLength))
                        .ToArray(),
                                        sample.Details))
            .ToArray());

        var productTypeSuggestionShadow =
            new CatalogImportProductTypeSuggestionShadowResponse(
                result.Value.ProductTypeSuggestionShadow.RowsAnalyzedCount,
                result.Value.ProductTypeSuggestionShadow.HasSelectedProductType,
                result.Value.ProductTypeSuggestionShadow.SelectedProductTypeId,
                result.Value.ProductTypeSuggestionShadow.SelectedProductTypeCode,
                result.Value.ProductTypeSuggestionShadow.SelectedProductTypeName,
                result.Value.ProductTypeSuggestionShadow.SuggestedRowsCount,
                result.Value.ProductTypeSuggestionShadow.MatchesCount,
                result.Value.ProductTypeSuggestionShadow.ConflictsCount,
                result.Value.ProductTypeSuggestionShadow.SuggestionsCount,
                result.Value.ProductTypeSuggestionShadow.NameConflictsCount,
                result.Value.ProductTypeSuggestionShadow.UnresolvedCount,
                result.Value.ProductTypeSuggestionShadow.DistinctSuggestedProductTypesCount,
                result.Value.ProductTypeSuggestionShadow.HasMixedSuggestedProductTypes,
                result.Value.ProductTypeSuggestionShadow.SamplesTruncated,
                result.Value.ProductTypeSuggestionShadow.TypeGroups
                    .Select(group =>
                        new CatalogImportProductTypeSuggestionShadowGroupResponse(
                            group.ProductTypeId,
                            group.ProductTypeCode,
                            group.ProductTypeName,
                            group.RowsCount,
                            group.MatchesCount,
                            group.ConflictsCount,
                            group.SuggestionsCount,
                            group.HighestConfidence,
                            group.ExampleRowNumbers))
                    .ToArray(),
                result.Value.ProductTypeSuggestionShadow.Samples
                    .Select(sample =>
                        new CatalogImportProductTypeSuggestionShadowSampleResponse(
                            sample.RowNumber,
                            sample.Kind.ToString(),
                            sample.ProductName,
                            sample.SelectedProductTypeId,
                            sample.SelectedProductTypeCode,
                            sample.SelectedProductTypeName,
                            sample.SuggestedProductTypeId,
                            sample.SuggestedProductTypeCode,
                            sample.SuggestedProductTypeName,
                            sample.Confidence,
                            sample.HighestPriority,
                            sample.Candidates
                                .Select(candidate =>
                                    new CatalogImportProductTypeSuggestionShadowCandidateResponse(
                                        candidate.ProductTypeId,
                                        candidate.ProductTypeCode,
                                        candidate.ProductTypeName,
                                        candidate.HighestPriority,
                                        candidate.Confidence,
                                        candidate.Evidence
                                            .Select(evidence =>
                                                new CatalogImportProductTypeSuggestionShadowEvidenceResponse(
                                                    evidence.DictionaryTermId,
                                                    evidence.Phrase,
                                                    evidence.RawValue,
                                                    evidence.NormalizedValue,
                                                    evidence.Priority,
                                                    evidence.Source,
                                                    evidence.StartIndex,
                                                    evidence.Length,
                                                    evidence.EndIndex))
                                            .ToArray()))
                                .ToArray(),
                            sample.Details))
                    .ToArray());

        var productNameExplanation =
    new CatalogImportProductNameExplanationResponse(
        result.Value.ProductNameExplanation.RowsAnalyzedCount,
        result.Value.ProductNameExplanation.RowsWithEvidenceCount,
        result.Value.ProductNameExplanation.FullyExplainedRowsCount,
        result.Value.ProductNameExplanation.PartiallyExplainedRowsCount,
        result.Value.ProductNameExplanation.UnexplainedRowsCount,
        result.Value.ProductNameExplanation.AverageCoverage,
        result.Value.ProductNameExplanation.SamplesTruncated,
        result.Value.ProductNameExplanation.Samples
            .Select(sample =>
                new CatalogImportProductNameExplanationSampleResponse(
                    sample.RowNumber,
                    sample.Kind.ToString(),
                    sample.Explanation.ProductName,
                    sample.Explanation.MeaningfulCharactersCount,
                    sample.Explanation.CoveredMeaningfulCharactersCount,
                    sample.Explanation.Coverage,
                    sample.Explanation.IsFullyExplained,
                    sample.Explanation.HasUnexplainedSpans,
                    sample.ManufacturerEvidenceCount,
                    sample.ProductTypeEvidenceCount,
                    sample.CharacteristicEvidenceCount,
                    sample.Explanation.Evidence
                        .Select(evidence =>
                            new CatalogImportProductNameEvidenceSpanResponse(
                                evidence.Kind.ToString(),
                                evidence.TargetCode,
                                evidence.TargetValue,
                                evidence.RawValue,
                                evidence.Source,
                                evidence.Confidence,
                                evidence.Priority,
                                evidence.StartIndex,
                                evidence.Length,
                                evidence.EndIndex))
                        .ToArray(),
                    sample.Explanation.UnexplainedSpans
                        .Select(span =>
                            new CatalogImportProductNameUnexplainedSpanResponse(
                                span.RawValue,
                                span.StartIndex,
                                span.Length,
                                span.EndIndex))
                        .ToArray()))
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
                manufacturerRecognitionShadow,
                productTypeSuggestionShadow,
                recognitionShadow,
                productNameExplanation,
                recognitionEnrichment));
    }
}