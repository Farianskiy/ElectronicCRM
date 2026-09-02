namespace ElectronicService.Core.Catalog.Recognition.Management.UpdateProfile;

public sealed record UpdateCatalogCharacteristicRecognitionProfileCommand(
    Guid ProfileId,
    string StrategyKind,
    int Priority,
    decimal MinimumConfidence,
    string ConfigurationJson);