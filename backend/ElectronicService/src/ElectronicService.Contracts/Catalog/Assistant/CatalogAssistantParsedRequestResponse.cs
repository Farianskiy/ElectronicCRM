namespace ElectronicService.Contracts.Catalog.Assistant;

public sealed record CatalogAssistantParsedRequestResponse(
    string Intent,
    string? Search,
    string? ProductTypeCode,
    string? Manufacturer,
    IReadOnlyCollection<CatalogAssistantCharacteristicResponse> Characteristics,
    CatalogAssistantClarificationResponse? Clarification);