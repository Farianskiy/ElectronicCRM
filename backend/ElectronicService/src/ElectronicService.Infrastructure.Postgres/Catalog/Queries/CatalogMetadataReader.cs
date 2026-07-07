using System.Globalization;
using ElectronicService.Core.Catalog.Metadata.Abstractions;
using ElectronicService.Core.Catalog.Metadata.GetManufacturers;
using ElectronicService.Core.Catalog.Metadata.GetProductTypeCharacteristics;
using ElectronicService.Core.Catalog.Metadata.GetProductTypes;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Queries;

public sealed class CatalogMetadataReader : ICatalogMetadataReader
{
    private readonly ElectronicDbContext _dbContext;

    public CatalogMetadataReader(ElectronicDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<CatalogProductTypeResult>> GetProductTypesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProductTypes
            .AsNoTracking()
            .OrderBy(productType => productType.Name)
            .Select(productType => new CatalogProductTypeResult(
                productType.Id,
                productType.Code,
                productType.Name))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyCollection<CatalogProductTypeCharacteristicResult>> GetProductTypeCharacteristicsAsync(
        string productTypeCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(productTypeCode))
        {
            return [];
        }

        var normalizedProductTypeCode = NormalizeText(productTypeCode);

        return await (
            from productTypeCharacteristic in _dbContext.ProductTypeCharacteristics.AsNoTracking()
            join productType in _dbContext.ProductTypes.AsNoTracking()
                on productTypeCharacteristic.ProductTypeId equals productType.Id
            join definition in _dbContext.CharacteristicDefinitions.AsNoTracking()
                on productTypeCharacteristic.CharacteristicDefinitionId equals definition.Id
            where productType.Code == normalizedProductTypeCode
            orderby productTypeCharacteristic.IsRequired descending, definition.Name
            select new CatalogProductTypeCharacteristicResult(
                definition.Id,
                definition.Code,
                definition.Name,
                definition.DataType.ToString(),
                definition.Unit,
                productTypeCharacteristic.IsRequired,
                productTypeCharacteristic.IsFilterable,
                productTypeCharacteristic.IsUsedForReplacement,
                productTypeCharacteristic.ReplacementMatchMode.ToString(),
                productTypeCharacteristic.ReplacementWeight))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyCollection<CatalogManufacturerResult>> GetManufacturersAsync(
        string? search,
        CancellationToken cancellationToken = default)
    {
        var manufacturersQuery = _dbContext.Manufacturers
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = NormalizeText(search);
            var searchPattern = CreateLikePattern(normalizedSearch);

            manufacturersQuery = manufacturersQuery.Where(manufacturer =>
                EF.Functions.ILike(manufacturer.NormalizedName, searchPattern));
        }

        return await manufacturersQuery
            .OrderBy(manufacturer => manufacturer.NormalizedName)
            .Select(manufacturer => new CatalogManufacturerResult(
                manufacturer.Id,
                manufacturer.Name))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static string NormalizeText(string value)
    {
        return value
            .Trim()
            .ToUpperInvariant()
            .Replace("Ё", "Е", StringComparison.Ordinal);
    }

    private static string CreateLikePattern(string value)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"%{value.Trim()}%");
    }
}