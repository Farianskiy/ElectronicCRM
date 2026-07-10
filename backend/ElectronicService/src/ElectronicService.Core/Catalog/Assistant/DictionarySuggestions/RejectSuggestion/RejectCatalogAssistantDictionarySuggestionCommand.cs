namespace ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.RejectSuggestion;

public sealed record RejectCatalogAssistantDictionarySuggestionCommand(
    Guid SuggestionId,
    Guid ReviewedByUserId,
    string? ReviewComment);