namespace ElectronicService.Contracts.Catalog.Assistant;

public sealed record CatalogAssistantClarificationResponse(
    string UnknownPhrase,
    string SuggestedKind,
    string? SuggestedTargetCode,
    string SuggestedTargetValue,
    decimal Confidence,
    string Question);