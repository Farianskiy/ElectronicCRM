namespace ElectronicService.Contracts.Catalog.Recognition;

public sealed record CatalogRecognitionConflictResponse(
    string CharacteristicCode,
    IReadOnlyCollection<CatalogRecognizedCharacteristicResponse> Candidates);