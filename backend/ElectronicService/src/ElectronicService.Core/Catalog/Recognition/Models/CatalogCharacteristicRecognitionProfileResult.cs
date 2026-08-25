using ElectronicService.Domain.Catalog.Recognition;

namespace ElectronicService.Core.Catalog.Recognition.Models;

public sealed record CatalogCharacteristicRecognitionProfileResult(
    Guid Id,
    Guid ProductTypeId,
    Guid CharacteristicDefinitionId,
    string CharacteristicCode,
    string CharacteristicName,
    CatalogCharacteristicRecognitionStrategyKind StrategyKind,
    int Priority,
    decimal MinimumConfidence,
    string ConfigurationJson,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);