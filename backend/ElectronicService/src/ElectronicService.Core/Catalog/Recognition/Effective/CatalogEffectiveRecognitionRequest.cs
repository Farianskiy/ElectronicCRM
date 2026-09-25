using ElectronicService.Domain.Catalog.Characteristics;

namespace ElectronicService.Core.Catalog.Recognition.Effective;

public sealed record CatalogEffectiveRecognitionRequest(
    string ProductName,
    Guid? ManufacturerId,
    string? ManufacturerName,
    Guid? ProductTypeId,
    IReadOnlyCollection<CharacteristicDefinition>? AllowedCharacteristics,
    CatalogRecognitionRunContext Context);
