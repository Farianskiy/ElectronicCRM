namespace ElectronicService.Contracts.Catalog.Assistant.DictionarySuggestions;

public sealed record CreateCatalogAssistantDictionarySuggestionResponse(
    Guid Id,
    string Status,
    string Message);