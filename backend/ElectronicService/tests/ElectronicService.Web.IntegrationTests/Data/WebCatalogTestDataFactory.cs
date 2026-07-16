using System.Globalization;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.Manufacturers;
using ElectronicService.Domain.Catalog.Products;
using ElectronicService.Domain.Catalog.ProductTypes;
using ElectronicService.Infrastructure.Postgres.Data;
using ElectronicService.TestCommon;
using Microsoft.EntityFrameworkCore;
using ElectronicService.Domain.Catalog.ValueObjects;

namespace ElectronicService.Web.IntegrationTests.Data;

/// <summary>
/// Создаёт уникальный набор товаров в уже засеянном production seeder каталоге.
/// </summary>
internal static class WebCatalogTestDataFactory
{
    private const string ProductTypeCode =
        "MODULAR_CIRCUIT_BREAKER";

    private static readonly string[] RequiredDefinitionCodes =
    [
        "RATED_CURRENT",
        "POLES",
        "CURVE",
        "BREAKING_CAPACITY"
    ];

    public static string CreateMarker()
    {
        return string.Concat(
            "WEB-",
            Guid.NewGuid().ToString(
                "N",
                CultureInfo.InvariantCulture));
    }

    public static async Task<WebCatalogScenario> CreateScenarioAsync(
        ElectronicDbContext dbContext,
        string marker,
        CancellationToken cancellationToken,
        decimal? ratedCurrent = null)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        if (string.IsNullOrWhiteSpace(marker))
        {
            throw new ArgumentException(
                "Marker is required.",
                nameof(marker));
        }

        var effectiveRatedCurrent =
            ratedCurrent ?? CreateUniqueRatedCurrent(marker);

        var productType = await dbContext.ProductTypes
            .Include(type => type.Characteristics)
            .SingleAsync(
                type => EF.Functions.ILike(
                    type.Code,
                    ProductTypeCode),
                cancellationToken);

        // ProductTypeCharacteristic не содержит Code, поэтому определения
        // загружаются по фактическим связям типа товара.
        var definitions = await dbContext.CharacteristicDefinitions
            .Where(definition =>
                productType.Characteristics
                    .Select(characteristic =>
                        characteristic.CharacteristicDefinitionId)
                    .Contains(definition.Id))
            .ToDictionaryAsync(
                definition => definition.Code,
                StringComparer.Ordinal,
                cancellationToken);

        EnsureRequiredDefinitions(definitions);

        var iek = await GetOrCreateManufacturerAsync(
            dbContext,
            "IEK",
            cancellationToken);

        var ekf = await GetOrCreateManufacturerAsync(
            dbContext,
            "EKF",
            cancellationToken);

        var chint = await GetOrCreateManufacturerAsync(
            dbContext,
            "CHINT",
            cancellationToken);

        var source = CreateProduct(
            productType,
            definitions,
            iek,
            article: string.Concat(marker, "-SOURCE"),
            name: string.Concat(marker, " исходный автомат"),
            stockQuantity: 0m,
            ratedCurrent: effectiveRatedCurrent);

        AddAlias(
            source,
            string.Concat(marker, " alias"));

        var availableReplacement = CreateProduct(
            productType,
            definitions,
            ekf,
            article: string.Concat(marker, "-AVAILABLE"),
            name: string.Concat(marker, " доступный аналог"),
            stockQuantity: 12m,
            ratedCurrent: effectiveRatedCurrent);

        var unavailableReplacement = CreateProduct(
            productType,
            definitions,
            chint,
            article: string.Concat(marker, "-UNAVAILABLE"),
            name: string.Concat(marker, " отсутствующий аналог"),
            stockQuantity: 0m,
            ratedCurrent: effectiveRatedCurrent);

        dbContext.Products.AddRange(
            source,
            availableReplacement,
            unavailableReplacement);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new WebCatalogScenario(
            marker,
            source,
            availableReplacement,
            unavailableReplacement);
    }

    private static decimal CreateUniqueRatedCurrent(
        string marker)
    {
        var hash = StringComparer.Ordinal.GetHashCode(marker);
        var positiveHash = Math.Abs((long)hash);

        return 1_000m + (positiveHash % 100_000) / 10m;
    }

    private static Product CreateProduct(
        ProductType productType,
        Dictionary<string, CharacteristicDefinition> definitions,
        Manufacturer manufacturer,
        string article,
        string name,
        decimal stockQuantity,
        decimal ratedCurrent)
    {
        var product = TestDataFactory.CreateProduct(
            article,
            name,
            productType.Id,
            manufacturer.Id,
            price: 1_250m,
            stockQuantity: stockQuantity);

        SetCharacteristic(
            product,
            productType,
            definitions["RATED_CURRENT"],
            TestDataFactory.CreateNumberValue(ratedCurrent));

        SetCharacteristic(
            product,
            productType,
            definitions["POLES"],
            TestDataFactory.CreateNumberValue(2m));

        SetCharacteristic(
            product,
            productType,
            definitions["CURVE"],
            TestDataFactory.CreateTextValue("C"));

        SetCharacteristic(
            product,
            productType,
            definitions["BREAKING_CAPACITY"],
            TestDataFactory.CreateNumberValue(6m));

        return product;
    }

    private static void SetCharacteristic(
        Product product,
        ProductType productType,
        CharacteristicDefinition definition,
        CharacteristicValue value)
    {
        var result = product.SetCharacteristic(
            productType,
            definition,
            value);

        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Cannot set test characteristic '{definition.Code}': {result.Error.Code}: {result.Error.Message}"));
        }
    }

    private static void AddAlias(
        Product product,
        string alias)
    {
        var result = product.AddAlias(alias);

        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Cannot add test alias: {result.Error.Code}: {result.Error.Message}"));
        }
    }

    private static async Task<Manufacturer>
    GetOrCreateManufacturerAsync(
        ElectronicDbContext dbContext,
        string name,
        CancellationToken cancellationToken)
    {
        var existingManufacturer =
            await dbContext.Manufacturers
                .SingleOrDefaultAsync(
                    manufacturer =>
                        EF.Functions.ILike(
                            manufacturer.NormalizedName,
                            name),
                    cancellationToken);

        if (existingManufacturer is not null)
        {
            return existingManufacturer;
        }

        var manufacturerResult = Manufacturer.Create(name);

        if (manufacturerResult.IsFailure)
        {
            throw new InvalidOperationException(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Cannot create test manufacturer '{name}': " +
                    $"{manufacturerResult.Error.Code}: " +
                    $"{manufacturerResult.Error.Message}"));
        }

        var manufacturer = manufacturerResult.Value;

        dbContext.Manufacturers.Add(manufacturer);

        return manufacturer;
    }

    private static void EnsureRequiredDefinitions(
        Dictionary<string, CharacteristicDefinition> definitions)
    {
        foreach (var code in RequiredDefinitionCodes)
        {
            if (!definitions.ContainsKey(code))
            {
                throw new InvalidOperationException(
                    $"Seeded characteristic '{code}' was not found.");
            }
        }
    }
}

internal sealed record WebCatalogScenario(
    string Marker,
    Product Source,
    Product AvailableReplacement,
    Product UnavailableReplacement);
