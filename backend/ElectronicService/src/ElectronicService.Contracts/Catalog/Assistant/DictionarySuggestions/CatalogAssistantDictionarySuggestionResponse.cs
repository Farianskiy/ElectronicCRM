namespace ElectronicService.Contracts.Catalog.Assistant.DictionarySuggestions;

public sealed record CatalogAssistantDictionarySuggestionResponse(
    Guid Id,
    string OriginalMessage,
    string UnknownPhrase,
    string NormalizedUnknownPhrase,
    string SuggestedKind,
    string? SuggestedTargetCode,
    string SuggestedTargetValue,
    decimal Confidence,
    string Status,
    Guid CreatedByUserId,
    DateTime CreatedAtUtc,
    Guid? ReviewedByUserId,
    DateTime? ReviewedAtUtc,
    string? ReviewComment);