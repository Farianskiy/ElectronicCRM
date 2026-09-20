using ElectronicService.Core.Catalog.ImportBatches.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.ImportBatches.GetRowConfirmedSpans;

public sealed record CatalogImportTrainingExampleStateResponse(
    Guid ExampleId,
    string ProductName,
    string RawValue,
    string NormalizedValue,
    int SpanStart,
    int SpanLength,
    DateTime ConfirmedAtUtc,
    bool MatchesSavedFeedback);

public sealed record CatalogImportRowFeedbackSpanResponse(
    Guid CharacteristicDefinitionId,
    string ProductName,
    Guid? ManufacturerId,
    Guid ProductTypeId,
    string? FinalNormalizedValue,
    string? ConfirmedRawValue,
    int? ConfirmedSpanStart,
    int? ConfirmedSpanLength,
    bool IsFinalized,
    CatalogImportTrainingExampleStateResponse? TrainingExample);

[ApiController]
[PermissionAnyAuthorize(UserPermissionCode.CatalogImportsCreate, UserPermissionCode.CatalogImportsReview)]
[Route("api/catalog/import-batches")]
public sealed class GetCatalogImportRowConfirmedSpansController : ControllerBase
{
    [HttpGet("{batchId:guid}/rows/{rowId:guid}/confirmed-spans")]
    public async Task<ActionResult<IReadOnlyCollection<CatalogImportRowFeedbackSpanResponse>>> Get(
        Guid batchId,
        Guid rowId,
        [FromServices] ICatalogImportBatchRepository batchRepository,
        [FromServices] ICatalogRecognitionFeedbackRepository feedbackRepository,
        [FromServices] ICatalogRecognitionTrainingExampleRepository exampleRepository,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var currentUserId))
        {
            return Unauthorized();
        }

        var batch = await batchRepository.GetByIdAsync(batchId, cancellationToken).ConfigureAwait(false);

        if (batch is null)
        {
            return NotFound();
        }

        if (batch.CreatedByUserId != currentUserId)
        {
            return Forbid();
        }

        var row = await batchRepository.GetRowByIdAsync(batchId, rowId, cancellationToken).ConfigureAwait(false);

        if (row is null)
        {
            return NotFound();
        }

        var entries = await feedbackRepository.GetByImportRowAsync(rowId, cancellationToken).ConfigureAwait(false);
        var feedbackIds = entries.Select(entry => entry.Id).ToArray();
        var examples = await exampleRepository.GetActiveByFeedbackIdsAsync(feedbackIds, cancellationToken).ConfigureAwait(false);
        var examplesByFeedbackId = examples.ToDictionary(example => example.SourceFeedbackId);

        var response = entries
            .OrderBy(entry => entry.CharacteristicDefinitionId)
            .Select(entry => new CatalogImportRowFeedbackSpanResponse(
                entry.CharacteristicDefinitionId,
                entry.ProductName,
                entry.ManufacturerId,
                entry.ProductTypeId,
                entry.FinalNormalizedValue,
                entry.ConfirmedRawValue,
                entry.ConfirmedSpanStart,
                entry.ConfirmedSpanLength,
                entry.IsFinalized,
                BuildTrainingState(entry, examplesByFeedbackId.GetValueOrDefault(entry.Id))))
            .ToArray();

        return Ok(response);
    }

    private static CatalogImportTrainingExampleStateResponse? BuildTrainingState(CatalogRecognitionFeedback feedback, CatalogRecognitionTrainingExample? example)
    {
        if (example is null)
        {
            return null;
        }

        var matches = example.ManufacturerId == feedback.ManufacturerId
            && example.ProductTypeId == feedback.ProductTypeId
            && example.CharacteristicDefinitionId == feedback.CharacteristicDefinitionId
            && example.SpanStart == feedback.ConfirmedSpanStart
            && example.SpanLength == feedback.ConfirmedSpanLength
            && string.Equals(example.ProductName, feedback.ProductName, StringComparison.Ordinal)
            && string.Equals(example.RawValue, feedback.ConfirmedRawValue, StringComparison.Ordinal)
            && string.Equals(example.NormalizedValue, feedback.FinalNormalizedValue, StringComparison.Ordinal);

        return new CatalogImportTrainingExampleStateResponse(
            example.Id,
            example.ProductName,
            example.RawValue,
            example.NormalizedValue,
            example.SpanStart,
            example.SpanLength,
            example.ConfirmedAtUtc,
            matches);
    }
}