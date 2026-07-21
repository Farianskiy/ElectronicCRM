using System.Globalization;
using ElectronicService.Core.Catalog
    .CharacteristicDefinitions.Abstractions;
using ElectronicService.Core.Catalog
    .CharacteristicDefinitions.GetDefinitions;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres
    .Catalog.Queries;

public sealed class CatalogCharacteristicDefinitionsReader
    : ICatalogCharacteristicDefinitionsReader
{
    private readonly ElectronicDbContext _dbContext;

    public CatalogCharacteristicDefinitionsReader(
        ElectronicDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<
            CatalogCharacteristicDefinitionResult>>
        GetAsync(
            string? search,
            CancellationToken cancellationToken = default)
    {
        var definitionsQuery =
            _dbContext.CharacteristicDefinitions
                .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchPattern =
                CreateLikePattern(search);

            definitionsQuery =
                definitionsQuery.Where(definition =>
                    EF.Functions.ILike(
                        definition.Code,
                        searchPattern)
                    || EF.Functions.ILike(
                        definition.Name,
                        searchPattern));
        }

        return await definitionsQuery
            .OrderBy(definition => definition.Name)
            .ThenBy(definition => definition.Code)
            .Select(definition =>
                new CatalogCharacteristicDefinitionResult(
                    definition.Id,
                    definition.Code,
                    definition.Name,
                    definition.DataType.ToString(),
                    definition.Unit,

                    _dbContext.ProductTypeCharacteristics
                        .Count(typeCharacteristic =>
                            typeCharacteristic
                                .CharacteristicDefinitionId
                            == definition.Id),

                    _dbContext.ProductCharacteristics
                        .Count(productCharacteristic =>
                            productCharacteristic
                                .CharacteristicDefinitionId
                            == definition.Id)))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static string CreateLikePattern(
        string search)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"%{search.Trim()}%");
    }
}