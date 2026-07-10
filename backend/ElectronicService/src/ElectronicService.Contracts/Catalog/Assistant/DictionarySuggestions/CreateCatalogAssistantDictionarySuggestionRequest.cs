namespace ElectronicService.Contracts.Catalog.Assistant.DictionarySuggestions;

public sealed class CreateCatalogAssistantDictionarySuggestionRequest
{
    public string OriginalMessage { get; init; } = string.Empty;

    public string UnknownPhrase { get; init; } = string.Empty;

    public string SuggestedKind { get; init; } = string.Empty;

    public string? SuggestedTargetCode { get; init; }

    public string SuggestedTargetValue { get; init; } = string.Empty;

    public decimal Confidence { get; init; }
}