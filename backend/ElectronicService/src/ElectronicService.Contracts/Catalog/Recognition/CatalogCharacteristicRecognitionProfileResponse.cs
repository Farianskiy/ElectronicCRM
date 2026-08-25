namespace ElectronicService.Contracts.Catalog.Recognition;

public sealed record CatalogCharacteristicRecognitionProfileResponse(
    Guid Id,
    Guid ProductTypeId,
    Guid CharacteristicDefinitionId,
    string CharacteristicCode,
    string CharacteristicName,
    string StrategyKind,
    int Priority,
    decimal MinimumConfidence,
    string ConfigurationJson,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);