using ElectronicService.Core.Catalog.Manufacturers.Abstractions;
using ElectronicService.Domain.Catalog.Manufacturers;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Manufacturers;

public sealed class ManufacturerNoisePhraseRepository : IManufacturerNoisePhraseRepository
{
    private readonly ElectronicDbContext _dbContext;

    public ManufacturerNoisePhraseRepository(ElectronicDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public Task<ManufacturerNoisePhrase?> GetByNormalizedPhraseAsync(string normalizedPhrase, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedPhrase);

        return _dbContext.ManufacturerNoisePhrases.SingleOrDefaultAsync(
            noisePhrase => noisePhrase.NormalizedPhrase == normalizedPhrase,
            cancellationToken);
    }

    public Task<bool> ActiveExistsAsync(string normalizedPhrase, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedPhrase);

        return _dbContext.ManufacturerNoisePhrases
            .AsNoTracking()
            .AnyAsync(
                noisePhrase => noisePhrase.NormalizedPhrase == normalizedPhrase && noisePhrase.IsActive,
                cancellationToken);
    }

    public void Add(ManufacturerNoisePhrase noisePhrase)
    {
        ArgumentNullException.ThrowIfNull(noisePhrase);

        _dbContext.ManufacturerNoisePhrases.Add(noisePhrase);
    }
}