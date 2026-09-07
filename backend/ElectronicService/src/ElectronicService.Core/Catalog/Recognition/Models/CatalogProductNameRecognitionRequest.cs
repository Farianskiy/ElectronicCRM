namespace ElectronicService.Core.Catalog.Recognition.Models;

public sealed record CatalogProductNameRecognitionRequest(
    string ProductName,
    Guid? ProductTypeId = null,
    IReadOnlyCollection<string>? AllowedCharacteristicCodes = null,
    Guid? ManufacturerId = null);