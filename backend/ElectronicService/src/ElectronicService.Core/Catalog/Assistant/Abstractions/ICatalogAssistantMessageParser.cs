using ElectronicService.Core.Catalog.Assistant.AskCatalogAssistant;

namespace ElectronicService.Core.Catalog.Assistant.Abstractions;

public interface ICatalogAssistantMessageParser
{
    Task<CatalogAssistantParsedRequest> ParseAsync(
        string message,
        CancellationToken cancellationToken = default);
}