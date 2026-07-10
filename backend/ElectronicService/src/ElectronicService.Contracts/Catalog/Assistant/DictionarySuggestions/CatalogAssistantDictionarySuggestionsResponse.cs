namespace ElectronicService.Contracts.Catalog.Assistant.DictionarySuggestions;

public sealed record CatalogAssistantDictionarySuggestionsResponse(
    IReadOnlyCollection<CatalogAssistantDictionarySuggestionResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);