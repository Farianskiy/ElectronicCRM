namespace ElectronicService.Contracts.Catalog.Assistant.DictionarySuggestions;

public sealed record CatalogAssistantDictionarySuggestionEvidenceExampleResponse(
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