namespace ElectronicService.Contracts.Catalog.Assistant;

public sealed record CatalogAssistantManufacturerRecognitionCandidateResponse(
    string ManufacturerName,
    string RawValue,
    string NormalizedValue,
    decimal Confidence,
    string Source,
    int StartIndex,
    int Length,
    int EndIndex);