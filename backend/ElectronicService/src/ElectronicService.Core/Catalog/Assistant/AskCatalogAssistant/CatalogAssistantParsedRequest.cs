using ElectronicService.Core.Catalog.Products.SearchProducts;

namespace ElectronicService.Core.Catalog.Assistant.AskCatalogAssistant;

public sealed record CatalogAssistantParsedRequest(
    CatalogAssistantIntent Intent,
    string? Search,
    string? ProductTypeCode,
    string? Manufacturer,
    IReadOnlyCollection<SearchProductCharacteristicFilter> Characteristics,
    CatalogAssistantClarificationResult? Clarification);