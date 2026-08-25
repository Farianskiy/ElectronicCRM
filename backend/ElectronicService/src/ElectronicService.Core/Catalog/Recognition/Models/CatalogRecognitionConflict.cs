namespace ElectronicService.Core.Catalog.Recognition.Models;

public sealed record CatalogRecognitionConflict(
    string CharacteristicCode,
    IReadOnlyCollection<CatalogRecognizedCharacteristic> Candidates);