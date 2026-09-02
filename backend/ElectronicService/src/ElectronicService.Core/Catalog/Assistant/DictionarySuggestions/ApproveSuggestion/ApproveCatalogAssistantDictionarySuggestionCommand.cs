namespace ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.ApproveSuggestion;

public sealed record ApproveCatalogAssistantDictionarySuggestionCommand(
    Guid SuggestionId,
    string Phrase,
    string Kind,
    string? TargetCode,
    string TargetValue,
    string? ProductTypeCode,
    int Priority,
    string? ReviewComment);