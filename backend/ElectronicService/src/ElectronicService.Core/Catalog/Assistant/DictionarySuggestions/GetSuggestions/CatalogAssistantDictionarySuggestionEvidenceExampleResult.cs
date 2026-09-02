namespace ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.GetSuggestions;

public sealed record CatalogAssistantDictionarySuggestionEvidenceExampleResult(
    Guid FeedbackId,
    string ProductName,
    string FeedbackType,
    string? SuggestedRawValue,
    string? SuggestedNormalizedValue,
    string? FinalNormalizedValue,
    decimal? SuggestedConfidence,
    string? SuggestedSource,
    int? SpanStart,
    int? SpanLength,
    string LabelQuality,
    DateTime? FinalizedAtUtc);