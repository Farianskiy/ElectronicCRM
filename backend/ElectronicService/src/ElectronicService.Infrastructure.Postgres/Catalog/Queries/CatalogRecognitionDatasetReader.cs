using System.Runtime.CompilerServices;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Datasets;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Queries;

public sealed class CatalogRecognitionDatasetReader : ICatalogRecognitionDatasetReader
{
    private readonly ElectronicDbContext _dbContext;

    public CatalogRecognitionDatasetReader(ElectronicDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public IAsyncEnumerable<CatalogRecognitionDatasetRecord> StreamTrainingEligibleAsync(DateTime finalizedUntilUtc, CancellationToken cancellationToken = default)
    {
        if (finalizedUntilUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Dataset cutoff must use UTC.", nameof(finalizedUntilUtc));
        }

        return StreamTrainingEligibleCoreAsync(finalizedUntilUtc, cancellationToken);
    }

    private async IAsyncEnumerable<CatalogRecognitionDatasetRecord> StreamTrainingEligibleCoreAsync(DateTime finalizedUntilUtc, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var query = _dbContext.CatalogRecognitionFeedbackEntries
            .AsNoTracking()
            .Where(feedback =>
                feedback.Status == CatalogRecognitionFeedbackStatus.Finalized &&
                feedback.IsTrainingEligible &&
                feedback.FinalizedAtUtc.HasValue &&
                feedback.FinalizedAtUtc.Value <= finalizedUntilUtc)
            .OrderBy(feedback => feedback.FinalizedAtUtc)
            .ThenBy(feedback => feedback.Id)
            .Select(feedback => new CatalogRecognitionDatasetRow(
                feedback.Id,
                feedback.ProductName,
                feedback.NormalizedProductName,
                feedback.ProductTypeId,
                feedback.ProductTypeCodeSnapshot,
                feedback.CharacteristicDefinitionId,
                feedback.CharacteristicCodeSnapshot,
                feedback.FeedbackType,
                feedback.LabelQuality,
                feedback.SuggestedRawValue,
                feedback.SuggestedNormalizedValue,
                feedback.SuggestedConfidence,
                feedback.SuggestedSource,
                feedback.SpanStart,
                feedback.SpanLength,
                feedback.FinalNormalizedValue,
                feedback.DictionaryTermId,
                feedback.RecognitionProfileId,
                feedback.ModelVersion,
                feedback.ImportBatchId,
                feedback.ImportRowId,
                feedback.ReviewerRole,
                feedback.CreatedAtUtc,
                feedback.FinalizedAtUtc))
            .AsAsyncEnumerable();

        await foreach (var row in query.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            yield return MapToDatasetRecord(row);
        }
    }

    private static CatalogRecognitionDatasetRecord MapToDatasetRecord(CatalogRecognitionDatasetRow row)
    {
        var finalizedAtUtc = row.FinalizedAtUtc ?? throw new InvalidOperationException($"Finalized feedback '{row.FeedbackId}' does not have FinalizedAtUtc.");

        return new CatalogRecognitionDatasetRecord(
            row.FeedbackId,
            row.ProductName,
            row.NormalizedProductName,
            row.ProductTypeId,
            row.ProductTypeCode,
            row.CharacteristicDefinitionId,
            row.CharacteristicCode,
            DetermineExampleKind(row),
            row.FeedbackType,
            row.LabelQuality,
            row.SuggestedRawValue,
            row.SuggestedNormalizedValue,
            row.SuggestedConfidence,
            row.SuggestedSource,
            row.SpanStart,
            row.SpanLength,
            row.FinalNormalizedValue,
            row.DictionaryTermId,
            row.RecognitionProfileId,
            row.ModelVersion,
            row.ImportBatchId,
            row.ImportRowId,
            row.ReviewerRole,
            row.CreatedAtUtc,
            finalizedAtUtc);
    }

    private static CatalogRecognitionDatasetExampleKind DetermineExampleKind(CatalogRecognitionDatasetRow row)
    {
        var hasSuggestedSpan = row.SpanStart.HasValue && row.SpanLength.HasValue && row.SuggestedRawValue is not null;

        return row.FeedbackType switch
        {
            CatalogRecognitionFeedbackType.Accepted when hasSuggestedSpan => CatalogRecognitionDatasetExampleKind.AcceptedSpan,
            CatalogRecognitionFeedbackType.Accepted => CatalogRecognitionDatasetExampleKind.AcceptedValue,
            CatalogRecognitionFeedbackType.Corrected => CatalogRecognitionDatasetExampleKind.CorrectedValue,
            CatalogRecognitionFeedbackType.Rejected when hasSuggestedSpan => CatalogRecognitionDatasetExampleKind.RejectedSpan,
            CatalogRecognitionFeedbackType.Rejected => CatalogRecognitionDatasetExampleKind.RejectedValue,
            CatalogRecognitionFeedbackType.AddedManually => CatalogRecognitionDatasetExampleKind.ManualValue,
            CatalogRecognitionFeedbackType.ConflictResolved => CatalogRecognitionDatasetExampleKind.ConflictResolution,
            _ => throw new InvalidOperationException($"Finalized feedback '{row.FeedbackId}' has unsupported feedback type '{row.FeedbackType}'.")
        };
    }

    private sealed record CatalogRecognitionDatasetRow(
        Guid FeedbackId,
        string ProductName,
        string NormalizedProductName,
        Guid ProductTypeId,
        string ProductTypeCode,
        Guid CharacteristicDefinitionId,
        string CharacteristicCode,
        CatalogRecognitionFeedbackType FeedbackType,
        CatalogRecognitionLabelQuality LabelQuality,
        string? SuggestedRawValue,
        string? SuggestedNormalizedValue,
        decimal? SuggestedConfidence,
        string? SuggestedSource,
        int? SpanStart,
        int? SpanLength,
        string? FinalNormalizedValue,
        Guid? DictionaryTermId,
        Guid? RecognitionProfileId,
        string? ModelVersion,
        Guid? ImportBatchId,
        Guid? ImportRowId,
        string? ReviewerRole,
        DateTime CreatedAtUtc,
        DateTime? FinalizedAtUtc);
}