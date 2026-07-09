using ElectronicService.Core.Catalog.Assistant.AskCatalogAssistant;

namespace ElectronicService.Core.Catalog.Assistant.Abstractions;

public interface ICatalogAssistantUnknownTermResolver
{
    Task<CatalogAssistantClarificationResult?> ResolveAsync(
        string unknownPhrase,
        CancellationToken cancellationToken = default);
}