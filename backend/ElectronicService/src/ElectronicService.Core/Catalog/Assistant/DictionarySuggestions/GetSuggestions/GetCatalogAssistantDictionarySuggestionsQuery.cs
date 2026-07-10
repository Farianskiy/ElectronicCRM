namespace ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.GetSuggestions;

public sealed record GetCatalogAssistantDictionarySuggestionsQuery(
    Guid TechnicalUserId,
    string? Status,
    int Page,
    int PageSize);