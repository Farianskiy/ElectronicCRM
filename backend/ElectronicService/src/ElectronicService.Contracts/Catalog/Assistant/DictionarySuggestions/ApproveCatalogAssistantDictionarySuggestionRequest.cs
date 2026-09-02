namespace ElectronicService.Contracts.Catalog.Assistant.DictionarySuggestions;

public sealed class ApproveCatalogAssistantDictionarySuggestionRequest
{
    public string Phrase { get; init; } = string.Empty;

    public string Kind { get; init; } = string.Empty;

    public string? TargetCode { get; init; }

    public string TargetValue { get; init; } = string.Empty;

    public string? ProductTypeCode { get; init; }

    public int Priority { get; init; }

    public string? ReviewComment { get; init; }
}