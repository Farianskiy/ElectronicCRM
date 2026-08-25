using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Repositories;

public sealed class CatalogCharacteristicRecognitionProfileRepository : ICatalogCharacteristicRecognitionProfileRepository
{
    private readonly ElectronicDbContext _dbContext;

    public CatalogCharacteristicRecognitionProfileRepository(ElectronicDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public Task<CatalogCharacteristicRecognitionProfile?> GetByIdAsync(
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        if (profileId == Guid.Empty)
        {
            return Task.FromResult<CatalogCharacteristicRecognitionProfile?>(null);
        }

        return _dbContext.CatalogCharacteristicRecognitionProfiles
            .FirstOrDefaultAsync(
                profile => profile.Id == profileId,
                cancellationToken);
    }

    public Task<bool> ExistsAsync(
        Guid productTypeId,
        Guid characteristicDefinitionId,
        CatalogCharacteristicRecognitionStrategyKind strategyKind,
        Guid? excludedProfileId = null,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.CatalogCharacteristicRecognitionProfiles
            .AsNoTracking()
            .AnyAsync(
                profile =>
                    profile.ProductTypeId == productTypeId &&
                    profile.CharacteristicDefinitionId == characteristicDefinitionId &&
                    profile.StrategyKind == strategyKind &&
                    (!excludedProfileId.HasValue || profile.Id != excludedProfileId.Value),
                cancellationToken);
    }

    public void Add(CatalogCharacteristicRecognitionProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        _dbContext.CatalogCharacteristicRecognitionProfiles.Add(profile);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}