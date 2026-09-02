namespace ElectronicService.Contracts.Catalog.Assistant;

public sealed record CatalogAssistantManufacturerRecognitionResponse(
    string ProductName,
    string Status,
    bool IsResolved,
    bool IsConflict,
    CatalogAssistantManufacturerRecognitionCandidateResponse? SelectedCandidate,
    IReadOnlyCollection<CatalogAssistantManufacturerRecognitionCandidateResponse> Candidates);