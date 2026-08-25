namespace ElectronicService.Contracts.Catalog.Recognition.Management;

public sealed record UpdateCatalogCharacteristicRecognitionProfileRequest(
    string StrategyKind,
    int Priority,
    decimal MinimumConfidence,
    string ConfigurationJson);