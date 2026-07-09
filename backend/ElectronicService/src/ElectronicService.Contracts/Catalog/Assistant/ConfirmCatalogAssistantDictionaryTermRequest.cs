namespace ElectronicService.Contracts.Catalog.Assistant;

public sealed class ConfirmCatalogAssistantDictionaryTermRequest
{
    public string Phrase { get; init; } = string.Empty;

    public string Kind { get; init; } = string.Empty;

    public string? TargetCode { get; init; }

    public string TargetValue { get; init; } = string.Empty;

    public int Priority { get; init; } = 100;
}