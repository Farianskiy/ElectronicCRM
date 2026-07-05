using ElectronicService.Domain.Catalog.Characteristics;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Seeding;

internal sealed record CatalogCharacteristicSeed(
    string Code,
    string Name,
    CharacteristicDataType DataType,
    string? Unit);