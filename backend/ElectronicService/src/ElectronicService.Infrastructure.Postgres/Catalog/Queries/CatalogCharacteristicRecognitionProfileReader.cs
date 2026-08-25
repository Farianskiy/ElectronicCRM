using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Models;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Queries;

public sealed class CatalogCharacteristicRecognitionProfileReader : ICatalogCharacteristicRecognitionProfileReader
{
    private readonly ElectronicDbContext _dbContext;

    public CatalogCharacteristicRecognitionProfileReader(ElectronicDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<CatalogCharacteristicRecognitionProfileResult>> GetProfilesAsync(
        Guid productTypeId,
        CancellationToken cancellationToken = default)
    {
        if (productTypeId == Guid.Empty)
        {
            return [];
        }

        return await (
            from profile in _dbContext.CatalogCharacteristicRecognitionProfiles.AsNoTracking()
            join characteristicDefinition in _dbContext.CharacteristicDefinitions.AsNoTracking()
                on profile.CharacteristicDefinitionId equals characteristicDefinition.Id
            where profile.ProductTypeId == productTypeId
            orderby characteristicDefinition.Code, profile.Priority descending, profile.Id
            select new CatalogCharacteristicRecognitionProfileResult(
                profile.Id,
                profile.ProductTypeId,
                profile.CharacteristicDefinitionId,
                characteristicDefinition.Code,
                characteristicDefinition.Name,
                profile.StrategyKind,
                profile.Priority,
                profile.MinimumConfidence,
                profile.ConfigurationJson,
                profile.IsActive,
                profile.CreatedAtUtc,
                profile.UpdatedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}