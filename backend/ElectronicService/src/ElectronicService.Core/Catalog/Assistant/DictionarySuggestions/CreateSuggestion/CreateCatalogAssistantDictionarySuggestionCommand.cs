namespace ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.CreateSuggestion;

public sealed record CreateCatalogAssistantDictionarySuggestionCommand(
    string OriginalMessage,
    string UnknownPhrase,
    string SuggestedKind,
    string? SuggestedTargetCode,
    string SuggestedTargetValue,
    decimal Confidence,
    Guid CreatedByUserId);