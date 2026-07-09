namespace ElectronicService.Contracts.Catalog.Assistant;

public sealed class AskCatalogAssistantRequest
{
    public string Message { get; init; } = string.Empty;

    public bool OnlyInStock { get; init; }

    public decimal MinimumScore { get; init; } = 70;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}