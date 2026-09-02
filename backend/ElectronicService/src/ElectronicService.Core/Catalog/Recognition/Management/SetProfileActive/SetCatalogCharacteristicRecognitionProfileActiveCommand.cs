namespace ElectronicService.Core.Catalog.Recognition.Management.SetProfileActive;

public sealed record SetCatalogCharacteristicRecognitionProfileActiveCommand(
    Guid ProfileId,
    bool IsActive);