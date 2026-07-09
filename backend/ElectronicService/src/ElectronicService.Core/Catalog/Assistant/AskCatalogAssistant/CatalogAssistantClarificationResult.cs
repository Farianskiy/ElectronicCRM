namespace ElectronicService.Core.Catalog.Assistant.AskCatalogAssistant;

public sealed record CatalogAssistantClarificationResult(
    string UnknownPhrase,
    string SuggestedKind,
    string? SuggestedTargetCode,
    string SuggestedTargetValue,
    decimal Confidence,
    string Question);