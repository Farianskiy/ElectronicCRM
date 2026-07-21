using ElectronicService.Core.Catalog.ProductTypes.Abstractions;
using ElectronicService.Core.Catalog.ProductTypes
    .GetCharacteristicSchema;
using ElectronicService.Core.Catalog.ProductTypes
    .GetAvailableCharacteristicDefinitions;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Queries;

public sealed class CatalogProductTypeSchemaReader
    : ICatalogProductTypeSchemaReader
{
    private readonly ElectronicDbContext _dbContext;

    public CatalogProductTypeSchemaReader(
        ElectronicDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<
        CatalogProductTypeCharacteristicSchemaResult?>
        GetByCodeAsync(
            string productTypeCode,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(productTypeCode))
        {
            return null;
        }

        var normalizedProductTypeCode =
            NormalizeProductTypeCode(productTypeCode);

        var productType = await _dbContext.ProductTypes
            .AsNoTracking()
            .Where(type =>
                type.Code == normalizedProductTypeCode)
            .Select(type => new
            {
                type.Id,
                type.Code,
                type.Name
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (productType is null)
        {
            return null;
        }

        var productsCount = await _dbContext.Products
            .AsNoTracking()
            .CountAsync(
                product =>
                    product.ProductTypeId == productType.Id,
                cancellationToken)
            .ConfigureAwait(false);

        /*
         * Получаем количество заполненных значений,
         * сгруппированных по определению характеристики.
         *
         * Уникальный индекс ProductId +
         * CharacteristicDefinitionId гарантирует, что
         * одна строка означает один товар с заполненным
         * значением.
         */
        var valueCounts = await (
                from productCharacteristic
                    in _dbContext.ProductCharacteristics
                        .AsNoTracking()

                join product
                    in _dbContext.Products.AsNoTracking()
                    on productCharacteristic.ProductId
                    equals product.Id

                where product.ProductTypeId == productType.Id

                group productCharacteristic
                    by productCharacteristic
                        .CharacteristicDefinitionId
                    into characteristicGroup

                select new
                {
                    CharacteristicDefinitionId =
                        characteristicGroup.Key,

                    ProductsWithValueCount =
                        characteristicGroup.Count()
                })
            .ToDictionaryAsync(
                item => item.CharacteristicDefinitionId,
                item => item.ProductsWithValueCount,
                cancellationToken)
            .ConfigureAwait(false);

        var rawCharacteristics = await (
                from productTypeCharacteristic
                    in _dbContext.ProductTypeCharacteristics
                        .AsNoTracking()

                join definition
                    in _dbContext.CharacteristicDefinitions
                        .AsNoTracking()
                    on productTypeCharacteristic
                        .CharacteristicDefinitionId
                    equals definition.Id

                where productTypeCharacteristic.ProductTypeId
                    == productType.Id

                orderby
                    productTypeCharacteristic.IsRequired
                        descending,
                    definition.Name

                select new
                {
                    DefinitionId = definition.Id,
                    definition.Code,
                    definition.Name,
                    DataType =
                        definition.DataType.ToString(),
                    definition.Unit,

                    productTypeCharacteristic.IsRequired,
                    productTypeCharacteristic.IsFilterable,

                    productTypeCharacteristic
                        .IsUsedForReplacement,

                    ReplacementMatchMode =
                        productTypeCharacteristic
                            .ReplacementMatchMode
                            .ToString(),

                    productTypeCharacteristic
                        .ReplacementWeight
                })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var characteristics = rawCharacteristics
            .Select(characteristic =>
            {
                valueCounts.TryGetValue(
                    characteristic.DefinitionId,
                    out var productsWithValueCount);

                var productsWithoutValueCount = Math.Max(
                    productsCount - productsWithValueCount,
                    0);

                return new
                    CatalogProductTypeCharacteristicSchemaItemResult(
                        characteristic.DefinitionId,
                        characteristic.Code,
                        characteristic.Name,
                        characteristic.DataType,
                        characteristic.Unit,
                        characteristic.IsRequired,
                        characteristic.IsFilterable,
                        characteristic.IsUsedForReplacement,
                        characteristic.ReplacementMatchMode,
                        characteristic.ReplacementWeight,
                        productsWithValueCount,
                        productsWithoutValueCount);
            })
            .ToList();

        return new
            CatalogProductTypeCharacteristicSchemaResult(
                productType.Id,
                productType.Code,
                productType.Name,
                productsCount,
                characteristics);
    }

    public async Task<IReadOnlyCollection<
        AvailableCharacteristicDefinitionResult>?>
    GetAvailableDefinitionsAsync(
        string productTypeCode,
        string? search,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(productTypeCode))
        {
            return null;
        }

        var normalizedProductTypeCode =
            NormalizeProductTypeCode(productTypeCode);

        var productTypeId = await _dbContext.ProductTypes
            .AsNoTracking()
            .Where(productType =>
                productType.Code == normalizedProductTypeCode)
            .Select(productType => (Guid?)productType.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (productTypeId is null)
        {
            return null;
        }

        /*
         * Выбираем только те CharacteristicDefinition,
         * для которых ещё нет ProductTypeCharacteristic
         * выбранного типа.
         */
        var definitionsQuery =
            _dbContext.CharacteristicDefinitions
                .AsNoTracking()
                .Where(definition =>
                    !_dbContext.ProductTypeCharacteristics
                        .Any(productTypeCharacteristic =>
                            productTypeCharacteristic.ProductTypeId
                                == productTypeId.Value
                            && productTypeCharacteristic
                                .CharacteristicDefinitionId
                                == definition.Id));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchPattern =
                $"%{search.Trim()}%";

            definitionsQuery = definitionsQuery.Where(
                definition =>
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
                new AvailableCharacteristicDefinitionResult(
                    definition.Id,
                    definition.Code,
                    definition.Name,
                    definition.DataType.ToString(),
                    definition.Unit))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static string NormalizeProductTypeCode(
        string productTypeCode)
    {
        return productTypeCode
            .Trim()
            .ToUpperInvariant()
            .Replace(
                "Ё",
                "Е",
                StringComparison.Ordinal)
            .Replace(
                " ",
                "_",
                StringComparison.Ordinal)
            .Replace(
                "-",
                "_",
                StringComparison.Ordinal);
    }
}