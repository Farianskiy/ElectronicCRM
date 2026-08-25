namespace ElectronicService.Core.Catalog.Recognition.Models;

public sealed record CatalogRecognizedCharacteristic(
    string CharacteristicCode,
    string RawValue,
    string NormalizedValue,
    decimal Confidence,
    CatalogRecognitionSource Source,
    int StartIndex,
    int Length,
    int Priority,
    string RecognizerKey);