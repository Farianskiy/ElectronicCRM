namespace ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.GetSuggestions;

public sealed record GetCatalogAssistantDictionarySuggestionsQuery(
    string? Status,
    int Page,
    int PageSize);