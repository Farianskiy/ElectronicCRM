using ElectronicService.Domain.Catalog.Manufacturers;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Seeding;

public sealed partial class CatalogDataSeeder
{
    private static readonly CatalogManufacturerSeed[] Manufacturers =
    [
        new("IEK"),
        new("EKF"),
        new("CHINT"),
        new("ABB"),
        new("DEKraft"),
        new("КЭАЗ"),
        new("ТДМ"),
        new("DKC"),
        new("Legrand"),
        new("LSIS"),
        new("Schneider Electric"),
        new("Systeme El"),
        new("C&S Electric"),
        new("Hyundai")
    ];

    private static readonly CatalogManufacturerAliasSeed[] ManufacturerAliases =
    [
        new("ИЭК", "IEK"),
        new("ИЕК", "IEK"),

        new("ЕКФ", "EKF"),

        new("ЧИНТ", "CHINT"),
        new("ЧЕНТ", "CHINT"),
        new("ЧНТ", "CHINT"),
        new("ЧАНТ", "CHINT"),

        new("АВВ", "ABB"),

        new("KEAZ", "КЭАЗ"),
        new("КЕАЗ", "КЭАЗ"),

        new("TDM", "ТДМ"),

        new("ДКС", "DKC"),

        new("ШНАЙДЕР", "Schneider Electric"),

        new("Systeme Electric", "Systeme El"),

        new("C&S", "C&S Electric")
    ];

    private async Task SeedManufacturersAsync(CancellationToken cancellationToken)
    {
        var existingManufacturers = await _dbContext.Manufacturers
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var manufacturersByNormalizedName = existingManufacturers
            .GroupBy(manufacturer => manufacturer.NormalizedName, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        foreach (var manufacturerName in Manufacturers.Select(seed => seed.Name))
        {
            var normalizedManufacturerName = ManufacturerNameNormalizer.Normalize(manufacturerName);

            if (manufacturersByNormalizedName.ContainsKey(normalizedManufacturerName))
            {
                continue;
            }

            var manufacturerResult = Manufacturer.Create(manufacturerName);

            if (manufacturerResult.IsFailure)
            {
                throw new InvalidOperationException(manufacturerResult.Error.Message);
            }

            var manufacturer = manufacturerResult.Value;

            await _dbContext.Manufacturers.AddAsync(manufacturer, cancellationToken).ConfigureAwait(false);

            manufacturersByNormalizedName.Add(manufacturer.NormalizedName, manufacturer);
        }
    }

    private async Task SeedManufacturerAliasesAsync(
        CancellationToken cancellationToken)
    {
        var manufacturers = await _dbContext.Manufacturers
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var manufacturersByNormalizedName = manufacturers
            .GroupBy(
                manufacturer => manufacturer.NormalizedName,
                StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.First(),
                StringComparer.Ordinal);

        var canonicalManufacturerNames = manufacturers
            .Select(manufacturer => manufacturer.NormalizedName)
            .ToHashSet(StringComparer.Ordinal);

        var existingActiveNormalizedPhrases =
            await _dbContext.ManufacturerAliases
                .AsNoTracking()
                .Where(manufacturerAlias =>
                    manufacturerAlias.Status ==
                    ManufacturerAliasStatus.Pending
                    ||
                    manufacturerAlias.Status ==
                    ManufacturerAliasStatus.Approved)
                .Select(manufacturerAlias =>
                    manufacturerAlias.NormalizedPhrase)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        var activeNormalizedPhraseSet =
            existingActiveNormalizedPhrases.ToHashSet(
                StringComparer.Ordinal);

        foreach (var seed in ManufacturerAliases)
        {
            var normalizedTargetManufacturerName =
                ManufacturerNameNormalizer.Normalize(
                    seed.ManufacturerName);

            if (!manufacturersByNormalizedName.TryGetValue(
                    normalizedTargetManufacturerName,
                    out var manufacturer))
            {
                throw new InvalidOperationException(
                    $"Manufacturer '{seed.ManufacturerName}' was not found while seeding alias '{seed.Phrase}'.");
            }

            var normalizedPhrase =
                ManufacturerNameNormalizer.Normalize(seed.Phrase);

            if (canonicalManufacturerNames.Contains(
                    normalizedPhrase))
            {
                if (string.Equals(
                        normalizedPhrase,
                        manufacturer.NormalizedName,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                throw new InvalidOperationException(
                    $"Manufacturer alias '{seed.Phrase}' conflicts with an existing canonical manufacturer name.");
            }

            if (!activeNormalizedPhraseSet.Add(
                    normalizedPhrase))
            {
                continue;
            }

            var manufacturerAliasResult =
                ManufacturerAlias.Create(
                    manufacturer.Id,
                    seed.Phrase,
                    ManufacturerAliasStatus.Approved,
                    ManufacturerAliasSource.Seed);

            if (manufacturerAliasResult.IsFailure)
            {
                throw new InvalidOperationException(
                    manufacturerAliasResult.Error.Message);
            }

            await _dbContext.ManufacturerAliases
                .AddAsync(
                    manufacturerAliasResult.Value,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private sealed record CatalogManufacturerSeed(
        string Name);

    private sealed record CatalogManufacturerAliasSeed(
        string Phrase,
        string ManufacturerName);
}