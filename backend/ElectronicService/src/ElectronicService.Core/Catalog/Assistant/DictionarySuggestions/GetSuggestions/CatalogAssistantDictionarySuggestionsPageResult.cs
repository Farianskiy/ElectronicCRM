namespace ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.GetSuggestions;

public sealed record CatalogAssistantDictionarySuggestionsPageResult(
    IReadOnlyCollection<CatalogAssistantDictionarySuggestionResult> Items,
    int Page,
    int PageSize,
    int TotalCount);