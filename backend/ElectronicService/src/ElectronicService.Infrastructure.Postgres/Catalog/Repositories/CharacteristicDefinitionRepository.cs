using ElectronicService.Core.Catalog
    .CharacteristicDefinitions.Abstractions;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres
    .Catalog.Repositories;

public sealed class CharacteristicDefinitionRepository
    : ICharacteristicDefinitionRepository
{
    private readonly ElectronicDbContext _dbContext;

    public CharacteristicDefinitionRepository(
        ElectronicDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<CharacteristicDefinition?> GetByIdAsync(
        Guid characteristicDefinitionId,
        CancellationToken cancellationToken = default)
    {
        /*
         * AsNoTracking отсутствует:
         * definition будет изменяться.
         */
        return _dbContext.CharacteristicDefinitions
            .FirstOrDefaultAsync(
                definition =>
                    definition.Id
                        == characteristicDefinitionId,
                cancellationToken);
    }

    public Task<bool> ExistsByCodeAsync(
        string normalizedCode,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.CharacteristicDefinitions
            .AsNoTracking()
            .AnyAsync(
                definition =>
                    definition.Code == normalizedCode,
                cancellationToken);
    }

    public void Add(
        CharacteristicDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        _dbContext.CharacteristicDefinitions.Add(
            definition);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _dbContext
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}