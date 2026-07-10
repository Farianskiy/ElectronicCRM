namespace ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.ApproveSuggestion;

public sealed record ApproveCatalogAssistantDictionarySuggestionCommand(
    Guid SuggestionId,
    Guid ReviewedByUserId,
    string? ReviewComment);