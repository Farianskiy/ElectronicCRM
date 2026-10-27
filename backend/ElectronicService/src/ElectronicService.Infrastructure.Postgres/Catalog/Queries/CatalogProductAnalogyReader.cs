using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ElectronicService.Core.Catalog.Products.Analogies;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Queries;

public sealed class CatalogProductAnalogyReader
    : ICatalogProductAnalogyReader
{
    private const int MaximumAllowedCandidates = 10000;
    private static readonly TimeSpan SnapshotLifetime = TimeSpan.FromMinutes(2);

    private readonly ElectronicDbContext _dbContext;
    private readonly IMemoryCache _cache;

    public CatalogProductAnalogyReader(
        ElectronicDbContext dbContext,
        IMemoryCache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public async Task<CatalogProductAnalogyCandidateSnapshot>
        ReadCandidateSnapshotAsync(
            Guid manufacturerId,
            Guid? productTypeId,
            int maximumCandidates,
            CancellationToken cancellationToken = default)
    {
        if (manufacturerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Укажите производителя.",
                nameof(manufacturerId));
        }

        var take = Math.Clamp(
            maximumCandidates,
            1,
            MaximumAllowedCandidates);

        var productsQuery =
            from product in _dbContext.Products.AsNoTracking()
            join productType in _dbContext.ProductTypes.AsNoTracking()
                on product.ProductTypeId equals productType.Id
            where product.ManufacturerId == manufacturerId
                  && !product.IsArchived
            select new
            {
                Product = product,
                ProductType = productType
            };

        if (productTypeId.HasValue)
        {
            productsQuery = productsQuery.Where(item =>
                item.Product.ProductTypeId == productTypeId.Value);
        }

        var fingerprint = await productsQuery
            .GroupBy(_ => 1)
            .Select(group => new ScopeFingerprint(
                group.Count(),
                group.Max(item => item.Product.CreatedAtUtc),
                group.Max(item => item.Product.UpdatedAtUtc)))
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? new ScopeFingerprint(0, null, null);

        var snapshotId = CreateSnapshotId(
            manufacturerId,
            productTypeId,
            take,
            fingerprint);
        var snapshotCreatedAtUtc = new[]
            {
                fingerprint.LastCreatedAtUtc,
                fingerprint.LastUpdatedAtUtc
            }
            .Where(value => value.HasValue)
            .Max()
            ?? DateTime.UnixEpoch;
        var cacheKey = $"catalog-analogy:{snapshotId}";

        if (_cache.TryGetValue(
                cacheKey,
                out CatalogProductAnalogyCandidateSnapshot? cached)
            && cached is not null)
        {
            return cached;
        }

        var products = await productsQuery
            .OrderBy(item => item.Product.Name.NormalizedValue)
            .ThenBy(item => item.Product.Article.Value)
            .ThenBy(item => item.Product.Id)
            .Take(take)
            .Select(item => new StoredProduct(
                item.Product.Id,
                item.Product.Article.Value,
                item.Product.Name.Value,
                item.Product.ManufacturerId,
                item.Product.ProductTypeId,
                item.ProductType.Code,
                item.ProductType.Name))
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        if (products.Length == 0)
        {
            var emptySnapshot = new CatalogProductAnalogyCandidateSnapshot(
                snapshotId,
                snapshotCreatedAtUtc,
                []);
            Cache(cacheKey, emptySnapshot);
            return emptySnapshot;
        }

        var productIds = products
            .Select(product => product.Id)
            .ToArray();

        var aliases = await _dbContext.ProductAliases
            .AsNoTracking()
            .Where(alias => productIds.Contains(alias.ProductId))
            .OrderBy(alias => alias.NormalizedValue)
            .Select(alias => new
            {
                alias.ProductId,
                alias.Value
            })
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        var storedCharacteristics = await (
                from characteristic in
                    _dbContext.ProductCharacteristics.AsNoTracking()
                join definition in
                    _dbContext.CharacteristicDefinitions.AsNoTracking()
                    on characteristic.CharacteristicDefinitionId
                    equals definition.Id
                where productIds.Contains(characteristic.ProductId)
                orderby definition.Code
                select new StoredCharacteristic(
                    characteristic.ProductId,
                    definition.Code,
                    definition.DataType,
                    characteristic.Value.TextValue,
                    characteristic.Value.NumberValue,
                    characteristic.Value.BooleanValue))
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        var aliasesByProduct = aliases
            .GroupBy(alias => alias.ProductId)
            .ToDictionary(
                group => group.Key,
                group =>
                    (IReadOnlyCollection<string>)group
                        .Select(alias => alias.Value)
                        .ToArray());

        var characteristicsByProduct = storedCharacteristics
            .GroupBy(characteristic => characteristic.ProductId)
            .ToDictionary(
                group => group.Key,
                group =>
                    (IReadOnlyDictionary<string, string>)group
                        .ToDictionary(
                            characteristic => characteristic.Code,
                            characteristic => FormatValue(characteristic),
                            StringComparer.Ordinal));

        var candidates = products
            .Select(product =>
                new CatalogProductAnalogyCandidate(
                    product.Id,
                    product.Article,
                    product.Name,
                    product.ManufacturerId,
                    product.ProductTypeId,
                    product.ProductTypeCode,
                    product.ProductTypeName,
                    aliasesByProduct.GetValueOrDefault(product.Id) ?? [],
                    characteristicsByProduct.GetValueOrDefault(product.Id)
                        ?? new Dictionary<string, string>(
                            StringComparer.Ordinal),
                    IsArchived: false))
            .ToArray();

        var snapshot = new CatalogProductAnalogyCandidateSnapshot(
            snapshotId,
            snapshotCreatedAtUtc,
            candidates);
        Cache(cacheKey, snapshot);
        return snapshot;
    }

    private void Cache(
        string cacheKey,
        CatalogProductAnalogyCandidateSnapshot snapshot)
    {
        _cache.Set(
            cacheKey,
            snapshot,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = SnapshotLifetime
            });
    }

    private static string CreateSnapshotId(
        Guid manufacturerId,
        Guid? productTypeId,
        int take,
        ScopeFingerprint fingerprint)
    {
        var source = string.Create(
            CultureInfo.InvariantCulture,
            $"{manufacturerId:N}|{productTypeId:N}|{take}|{fingerprint.Count}|{fingerprint.LastCreatedAtUtc:O}|{fingerprint.LastUpdatedAtUtc:O}");
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(source));
        return Convert.ToHexString(hash)[..24];
    }

    private static string FormatValue(
        StoredCharacteristic characteristic)
    {
        return characteristic.DataType switch
        {
            CharacteristicDataType.Text =>
                characteristic.TextValue ?? string.Empty,
            CharacteristicDataType.Number =>
                characteristic.NumberValue?.ToString(
                    "0.####",
                    CultureInfo.InvariantCulture)
                ?? string.Empty,
            CharacteristicDataType.Boolean =>
                characteristic.BooleanValue is true ? "Да" : "Нет",
            _ => string.Empty
        };
    }

    private sealed record StoredProduct(
        Guid Id,
        string Article,
        string Name,
        Guid ManufacturerId,
        Guid ProductTypeId,
        string ProductTypeCode,
        string ProductTypeName);

    private sealed record StoredCharacteristic(
        Guid ProductId,
        string Code,
        CharacteristicDataType DataType,
        string? TextValue,
        decimal? NumberValue,
        bool? BooleanValue);

    private sealed record ScopeFingerprint(
        int Count,
        DateTime? LastCreatedAtUtc,
        DateTime? LastUpdatedAtUtc);
}
