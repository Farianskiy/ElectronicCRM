namespace ElectronicService.Contracts.Catalog.Recognition;

public sealed record CatalogRecognizedCharacteristicResponse(
    string CharacteristicCode,
    string RawValue,
    string NormalizedValue,
    decimal Confidence,
    string Source,
    int StartIndex,
    int Length,
    int Priority,
    string RecognizerKey);