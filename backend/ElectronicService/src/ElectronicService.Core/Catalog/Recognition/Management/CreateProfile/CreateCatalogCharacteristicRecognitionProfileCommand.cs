namespace ElectronicService.Core.Catalog.Recognition.Management.CreateProfile;

public sealed record CreateCatalogCharacteristicRecognitionProfileCommand(
    string ProductTypeCode,
    Guid CharacteristicDefinitionId,
    string StrategyKind,
    int Priority,
    decimal MinimumConfidence,
    string ConfigurationJson);