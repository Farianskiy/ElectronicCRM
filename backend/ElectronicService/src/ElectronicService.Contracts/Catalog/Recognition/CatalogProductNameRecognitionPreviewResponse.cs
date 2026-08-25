namespace ElectronicService.Contracts.Catalog.Recognition;

public sealed record CatalogProductNameRecognitionPreviewResponse(
    bool HasProductTypeScope,
    Guid? ProductTypeId,
    string? ProductTypeCode,
    string? ProductTypeName,
    IReadOnlyCollection<string>? AllowedCharacteristicCodes,
    IReadOnlyCollection<CatalogCharacteristicRecognitionProfileResponse> RecognitionProfiles,
    string ProductName,
    string NormalizedProductName,
    IReadOnlyCollection<CatalogRecognizedCharacteristicResponse> Characteristics,
    IReadOnlyCollection<CatalogRecognitionConflictResponse> Conflicts,
    IReadOnlyCollection<CatalogRecognizedCharacteristicResponse> Candidates);