using ElectronicService.Domain.Catalog.Recognition;

namespace ElectronicService.Core.Catalog.Recognition.Datasets;

public sealed record CatalogRecognitionDatasetRecord(
    Guid FeedbackId,
    string ProductName,
    string NormalizedProductName,
    Guid ProductTypeId,
    string ProductTypeCode,
    Guid CharacteristicDefinitionId,
    string CharacteristicCode,
    CatalogRecognitionDatasetExampleKind ExampleKind,
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
    DateTime FinalizedAtUtc)
{
    public bool HasSuggestedSpan => SpanStart.HasValue && SpanLength.HasValue && SuggestedRawValue is not null;

    public bool HasVerifiedAnswerSpan => ExampleKind == CatalogRecognitionDatasetExampleKind.AcceptedSpan;

    public int? SpanEnd => SpanStart.HasValue && SpanLength.HasValue ? SpanStart.Value + SpanLength.Value : null;
}