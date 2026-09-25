using ElectronicService.Core.Catalog.Recognition.Effective;

namespace ElectronicService.Core.Catalog.Recognition.Models;

public sealed record CatalogProductNameRecognitionRequest(
    string ProductName,
    Guid? ProductTypeId = null,
    IReadOnlyCollection<string>? AllowedCharacteristicCodes = null,
    Guid? ManufacturerId = null,
    CatalogRecognitionRunContext? Context = null);