using ElectronicService.Domain.Catalog.Recognition;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Seeding;

internal sealed record CatalogCharacteristicRecognitionProfileSeed(
    string ProductTypeCode,
    string CharacteristicCode,
    CatalogCharacteristicRecognitionStrategyKind StrategyKind,
    int Priority,
    decimal MinimumConfidence,
    string ConfigurationJson);