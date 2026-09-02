namespace ElectronicService.Core.Catalog.Recognition.Models;

public sealed record CatalogProductNameRecognitionResult(
    string ProductName,
    string NormalizedProductName,
    IReadOnlyCollection<CatalogRecognizedCharacteristic> Characteristics,
    IReadOnlyCollection<CatalogRecognitionConflict> Conflicts,
    IReadOnlyCollection<CatalogRecognizedCharacteristic> Candidates);