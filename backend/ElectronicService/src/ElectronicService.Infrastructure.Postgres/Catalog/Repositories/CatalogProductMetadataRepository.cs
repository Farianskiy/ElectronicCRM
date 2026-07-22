using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.ProductTypes;
using ElectronicService.Infrastructure.Postgres.Data;
using ElectronicService.Domain.Catalog.Manufacturers;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Repositories;

public sealed class CatalogProductMetadataRepository : ICatalogProductMetadataRepository
{
    private readonly ElectronicDbContext _dbContext;

    public CatalogProductMetadataRepository(ElectronicDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ProductType?> GetProductTypeByIdAsync(
        Guid productTypeId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ProductTypes
            .AsNoTracking()
            .Include(productType => productType.Characteristics)
            .FirstOrDefaultAsync(
                productType => productType.Id == productTypeId,
                cancellationToken);
    }

    public Task<CharacteristicDefinition?> GetCharacteristicDefinitionByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = NormalizeCode(code);

        return _dbContext.CharacteristicDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                definition => definition.Code == normalizedCode,
                cancellationToken);
    }

    public Task<Manufacturer?> GetManufacturerByIdAsync(
        Guid manufacturerId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Manufacturers
            .AsNoTracking()
            .FirstOrDefaultAsync(
                manufacturer => manufacturer.Id == manufacturerId,
                cancellationToken);
    }

    public async Task<
        IReadOnlyCollection<CharacteristicDefinition>>
    GetCharacteristicDefinitionsByIdsAsync(
        IReadOnlyCollection<Guid>
            characteristicDefinitionIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            characteristicDefinitionIds);

        var definitionIds = characteristicDefinitionIds
            .Where(definitionId =>
                definitionId != Guid.Empty)
            .Distinct()
            .ToArray();

        if (definitionIds.Length == 0)
        {
            return [];
        }

        return await _dbContext.CharacteristicDefinitions
            .AsNoTracking()
            .Where(definition =>
                definitionIds.Contains(definition.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static string NormalizeCode(string code)
    {
        return code
            .Trim()
            .ToUpperInvariant()
            .Replace("Ё", "Е", StringComparison.Ordinal);
    }
}