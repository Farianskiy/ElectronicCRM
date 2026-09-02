using ElectronicService.Core.Catalog.Recognition.Models;

namespace ElectronicService.Core.Catalog.Recognition.Preview;

public sealed record CatalogProductNameRecognitionPreviewResult(
    Guid? ProductTypeId,
    string? ProductTypeCode,
    string? ProductTypeName,
    IReadOnlyCollection<string>? AllowedCharacteristicCodes,
    IReadOnlyCollection<CatalogCharacteristicRecognitionProfileResult> RecognitionProfiles,
    CatalogProductNameRecognitionResult RecognitionResult);