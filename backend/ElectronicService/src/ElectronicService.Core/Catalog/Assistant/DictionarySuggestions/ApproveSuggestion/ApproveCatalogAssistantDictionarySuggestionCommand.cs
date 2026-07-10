namespace ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.ApproveSuggestion;

public sealed record ApproveCatalogAssistantDictionarySuggestionCommand(
    Guid SuggestionId,
    string? ReviewComment);