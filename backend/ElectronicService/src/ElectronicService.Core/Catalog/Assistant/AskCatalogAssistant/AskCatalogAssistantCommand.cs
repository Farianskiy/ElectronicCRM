namespace ElectronicService.Core.Catalog.Assistant.AskCatalogAssistant;

public sealed record AskCatalogAssistantCommand(
    string Message,
    bool OnlyInStock,
    decimal MinimumScore,
    int Page,
    int PageSize);