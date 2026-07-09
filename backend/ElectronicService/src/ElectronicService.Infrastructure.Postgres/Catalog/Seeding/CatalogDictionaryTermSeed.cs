using ElectronicService.Domain.Catalog.Dictionaries;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Seeding;

internal sealed record CatalogDictionaryTermSeed(
        string Phrase,
        CatalogDictionaryTermKind Kind,
        string? TargetCode,
        string TargetValue,
        int Priority);