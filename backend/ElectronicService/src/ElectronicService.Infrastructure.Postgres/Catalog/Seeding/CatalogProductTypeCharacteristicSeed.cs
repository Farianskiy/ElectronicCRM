using ElectronicService.Domain.Catalog.Characteristics;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Seeding;

internal sealed record CatalogProductTypeCharacteristicSeed(
    string ProductTypeCode,
    string CharacteristicCode,
    bool IsRequired,
    bool IsFilterable,
    bool IsUsedForReplacement,
    ReplacementMatchMode ReplacementMatchMode,
    int ReplacementWeight);