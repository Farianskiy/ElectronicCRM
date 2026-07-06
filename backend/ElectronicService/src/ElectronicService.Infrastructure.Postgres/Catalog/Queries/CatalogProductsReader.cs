using System.Globalization;
using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Core.Catalog.Products.GetProductById;
using ElectronicService.Core.Catalog.Products.GetProducts;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Queries;

public sealed class CatalogProductsReader : ICatalogProductsReader
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private readonly ElectronicDbContext _dbContext;

    public CatalogProductsReader(ElectronicDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CatalogProductsPageResult> GetProductsAsync(
        string? search,
        string? productTypeCode,
        string? manufacturer,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = Math.Max(page, 1);

        var normalizedPageSize = pageSize <= 0
            ? DefaultPageSize
            : Math.Clamp(pageSize, 1, MaxPageSize);

        var productsQuery =
            from product in _dbContext.Products.AsNoTracking()
            join productType in _dbContext.ProductTypes.AsNoTracking()
                on product.ProductTypeId equals productType.Id
            join productManufacturer in _dbContext.Manufacturers.AsNoTracking()
                on product.ManufacturerId equals productManufacturer.Id
            select new
            {
                Product = product,
                ProductType = productType,
                Manufacturer = productManufacturer
            };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = NormalizeText(search);
            var searchPattern = CreateLikePattern(normalizedSearch);
            var originalSearchPattern = CreateLikePattern(search);

            productsQuery = productsQuery.Where(item =>
                EF.Functions.ILike(item.Product.Name.NormalizedValue, searchPattern)
                || EF.Functions.ILike(item.Product.Article.Value, originalSearchPattern));
        }

        if (!string.IsNullOrWhiteSpace(productTypeCode))
        {
            var normalizedProductTypeCode = NormalizeText(productTypeCode);

            productsQuery = productsQuery.Where(item =>
                item.ProductType.Code == normalizedProductTypeCode);
        }

        if (!string.IsNullOrWhiteSpace(manufacturer))
        {
            var normalizedManufacturer = NormalizeText(manufacturer);
            var manufacturerPattern = CreateLikePattern(normalizedManufacturer);

            productsQuery = productsQuery.Where(item =>
                EF.Functions.ILike(item.Manufacturer.NormalizedName, manufacturerPattern));
        }

        var totalCount = await productsQuery
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = await productsQuery
            .OrderBy(item => item.Product.Name.NormalizedValue)
            .ThenBy(item => item.Product.Article.Value)
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(item => new CatalogProductListItemResult(
                item.Product.Id,
                item.Product.Article.Value,
                item.Product.Name.Value,
                item.ProductType.Code,
                item.ProductType.Name,
                item.Manufacturer.Name,
                item.Product.Price.Amount,
                item.Product.Price.Currency,
                item.Product.StockQuantity.Value))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new CatalogProductsPageResult(
            items,
            normalizedPage,
            normalizedPageSize,
            totalCount);
    }

    public async Task<CatalogProductDetailsResult?> GetProductByIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var product = await (
            from productEntity in _dbContext.Products.AsNoTracking()
            join productType in _dbContext.ProductTypes.AsNoTracking()
                on productEntity.ProductTypeId equals productType.Id
            join manufacturer in _dbContext.Manufacturers.AsNoTracking()
                on productEntity.ManufacturerId equals manufacturer.Id
            where productEntity.Id == productId
            select new
            {
                productEntity.Id,
                Article = productEntity.Article.Value,
                Name = productEntity.Name.Value,
                ProductTypeCode = productType.Code,
                ProductTypeName = productType.Name,
                ManufacturerName = manufacturer.Name,
                PriceAmount = productEntity.Price.Amount,
                PriceCurrency = productEntity.Price.Currency,
                StockQuantity = productEntity.StockQuantity.Value
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (product is null)
        {
            return null;
        }

        var rawCharacteristics = await (
            from productCharacteristic in _dbContext.ProductCharacteristics.AsNoTracking()
            join definition in _dbContext.CharacteristicDefinitions.AsNoTracking()
                on productCharacteristic.CharacteristicDefinitionId equals definition.Id
            where productCharacteristic.ProductId == productId
            orderby definition.Name
            select new
            {
                definition.Code,
                definition.Name,
                definition.DataType,
                definition.Unit,
                productCharacteristic.Value.TextValue,
                productCharacteristic.Value.NumberValue,
                productCharacteristic.Value.BooleanValue
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var characteristics = rawCharacteristics
            .Select(characteristic => new CatalogProductCharacteristicResult(
                characteristic.Code,
                characteristic.Name,
                characteristic.DataType.ToString(),
                characteristic.Unit,
                FormatCharacteristicValue(
                    characteristic.DataType,
                    characteristic.TextValue,
                    characteristic.NumberValue,
                    characteristic.BooleanValue)))
            .ToList();

        return new CatalogProductDetailsResult(
            product.Id,
            product.Article,
            product.Name,
            product.ProductTypeCode,
            product.ProductTypeName,
            product.ManufacturerName,
            product.PriceAmount,
            product.PriceCurrency,
            product.StockQuantity,
            characteristics);
    }

    private static string FormatCharacteristicValue(
        CharacteristicDataType dataType,
        string? textValue,
        decimal? numberValue,
        bool? booleanValue)
    {
        return dataType switch
        {
            CharacteristicDataType.Text => textValue ?? string.Empty,

            CharacteristicDataType.Number => numberValue?.ToString(
                CultureInfo.InvariantCulture) ?? string.Empty,

            CharacteristicDataType.Boolean => booleanValue is true ? "Да" : "Нет",

            _ => string.Empty
        };
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