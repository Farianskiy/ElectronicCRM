namespace ElectronicService.Contracts.Catalog.Recognition.Management;

public sealed record CreateCatalogCharacteristicRecognitionProfileRequest(
    string ProductTypeCode,
    Guid CharacteristicDefinitionId,
    string StrategyKind,
    int Priority,
    decimal MinimumConfidence,
    string ConfigurationJson);