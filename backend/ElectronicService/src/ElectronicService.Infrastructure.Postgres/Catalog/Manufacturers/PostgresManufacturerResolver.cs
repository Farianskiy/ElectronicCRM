using ElectronicService.Core.Catalog.Manufacturers.Resolution;
using ElectronicService.Domain.Catalog.Manufacturers;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Manufacturers;

public sealed class PostgresManufacturerResolver : IManufacturerResolver
{
    private readonly ElectronicDbContext _dbContext;

    public PostgresManufacturerResolver(ElectronicDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ManufacturerResolutionIndex> LoadIndexAsync(CancellationToken cancellationToken = default)
    {
        var manufacturerEntries = await _dbContext.Manufacturers
            .AsNoTracking()
            .Select(manufacturer => new ManufacturerResolutionEntry(
                manufacturer.Id,
                manufacturer.Name,
                manufacturer.NormalizedName,
                ManufacturerResolutionSource.ExactName,
                null))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var approvedAliasEntries = await _dbContext.ManufacturerAliases
            .AsNoTracking()
            .Where(manufacturerAlias => manufacturerAlias.Status == ManufacturerAliasStatus.Approved)
            .Join(
                _dbContext.Manufacturers.AsNoTracking(),
                manufacturerAlias => manufacturerAlias.ManufacturerId,
                manufacturer => manufacturer.Id,
                (manufacturerAlias, manufacturer) => new ManufacturerResolutionEntry(
                    manufacturer.Id,
                    manufacturer.Name,
                    manufacturerAlias.NormalizedPhrase,
                    ManufacturerResolutionSource.ApprovedAlias,
                    manufacturerAlias.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var noiseEntries = await _dbContext.ManufacturerNoisePhrases
            .AsNoTracking()
            .Where(noisePhrase => noisePhrase.IsActive)
            .Select(noisePhrase => new ManufacturerNoiseResolutionEntry(
                noisePhrase.Id,
                noisePhrase.NormalizedPhrase,
                noisePhrase.Reason))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var entries = new List<ManufacturerResolutionEntry>(manufacturerEntries.Count + approvedAliasEntries.Count);

        entries.AddRange(manufacturerEntries);
        entries.AddRange(approvedAliasEntries);

        return new ManufacturerResolutionIndex(entries, noiseEntries);
    }
}