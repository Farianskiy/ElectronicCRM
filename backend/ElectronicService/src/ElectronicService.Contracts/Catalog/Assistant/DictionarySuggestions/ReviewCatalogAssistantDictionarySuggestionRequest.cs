namespace ElectronicService.Contracts.Catalog.Assistant.DictionarySuggestions;

public sealed class ReviewCatalogAssistantDictionarySuggestionRequest
{
    public Guid ReviewedByUserId { get; init; }

    public string? ReviewComment { get; init; }
}