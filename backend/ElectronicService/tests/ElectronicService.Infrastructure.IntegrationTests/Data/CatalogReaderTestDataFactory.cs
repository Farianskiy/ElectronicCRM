using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.Manufacturers;
using ElectronicService.Domain.Catalog.Products;
using ElectronicService.Domain.Catalog.ProductTypes;
using ElectronicService.Infrastructure.Postgres.Data;
using ElectronicService.TestCommon;

namespace ElectronicService.Infrastructure.IntegrationTests.Data;

/// <summary>
/// Создаёт связанные наборы данных для integration-тестов
/// чтения каталога и подбора аналогов.
/// </summary>
internal static class CatalogReaderTestDataFactory
{
    public static async Task<ReaderCatalogData> CreateReaderCatalogAsync(
        ElectronicDbContext dbContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        var suffix = Guid.NewGuid().ToString("N");

        var iekManufacturer = CreateManufacturer(
            $"IEK Reader {suffix}");

        var ekfManufacturer = CreateManufacturer(
            $"EKF Reader {suffix}");

        var breakerType = TestDataFactory.CreateProductType(
            code: $"BREAKER_{suffix}",
            name: $"Автоматический выключатель {suffix}");

        var switchType = TestDataFactory.CreateProductType(
            code: $"SWITCH_{suffix}",
            name: $"Выключатель нагрузки {suffix}");

        var seriesDefinition =
            TestDataFactory.CreateCharacteristicDefinition(
                code: $"SERIES_{suffix}",
                name: $"Серия {suffix}",
                dataType: CharacteristicDataType.Text,
                unit: null);

        var currentDefinition =
            TestDataFactory.CreateCharacteristicDefinition(
                code: $"RATED_CURRENT_{suffix}",
                name: $"Номинальный ток {suffix}",
                dataType: CharacteristicDataType.Number,
                unit: "А");

        var auxiliaryDefinition =
            TestDataFactory.CreateCharacteristicDefinition(
                code: $"HAS_AUXILIARY_{suffix}",
                name: $"Дополнительный контакт {suffix}",
                dataType: CharacteristicDataType.Boolean,
                unit: null);

        AddFilterableCharacteristic(
            breakerType,
            seriesDefinition);

        AddFilterableCharacteristic(
            breakerType,
            currentDefinition);

        AddFilterableCharacteristic(
            breakerType,
            auxiliaryDefinition);

        var alpha = CreateProduct(
            article: $"ALPHA-{suffix}",
            name: $"Alpha Breaker {suffix}",
            productType: breakerType,
            manufacturer: iekManufacturer,
            price: 100m,
            stockQuantity: 10m);

        SetCharacteristic(
            alpha,
            breakerType,
            seriesDefinition,
            TestDataFactory.CreateTextValue("Proxima"));

        SetCharacteristic(
            alpha,
            breakerType,
            currentDefinition,
            TestDataFactory.CreateNumberValue(16.5m));

        SetCharacteristic(
            alpha,
            breakerType,
            auxiliaryDefinition,
            TestDataFactory.CreateBooleanValue(true));

        AddAlias(alpha, $"Special Reader Alias {suffix}");
        AddAlias(alpha, $"Alpha Alias {suffix}");

        var beta = CreateProduct(
            article: $"BETA-{suffix}",
            name: $"Beta Breaker {suffix}",
            productType: breakerType,
            manufacturer: iekManufacturer,
            price: 200m,
            stockQuantity: 0m);

        SetCharacteristic(
            beta,
            breakerType,
            seriesDefinition,
            TestDataFactory.CreateTextValue("Armata"));

        SetCharacteristic(
            beta,
            breakerType,
            currentDefinition,
            TestDataFactory.CreateNumberValue(25m));

        SetCharacteristic(
            beta,
            breakerType,
            auxiliaryDefinition,
            TestDataFactory.CreateBooleanValue(false));

        var gamma = CreateProduct(
            article: $"GAMMA-{suffix}",
            name: $"Gamma Switch {suffix}",
            productType: switchType,
            manufacturer: ekfManufacturer,
            price: 300m,
            stockQuantity: 5m);

        var delta = CreateProduct(
            article: $"DELTA-{suffix}",
            name: $"Delta Breaker {suffix}",
            productType: breakerType,
            manufacturer: ekfManufacturer,
            price: 400m,
            stockQuantity: 3m);

        SetCharacteristic(
            delta,
            breakerType,
            seriesDefinition,
            TestDataFactory.CreateTextValue("Proxima"));

        SetCharacteristic(
            delta,
            breakerType,
            currentDefinition,
            TestDataFactory.CreateNumberValue(16.5m));

        SetCharacteristic(
            delta,
            breakerType,
            auxiliaryDefinition,
            TestDataFactory.CreateBooleanValue(true));

        var epsilon = CreateProduct(
            article: $"EPSILON-{suffix}",
            name: $"Epsilon Switch {suffix}",
            productType: switchType,
            manufacturer: iekManufacturer,
            price: 500m,
            stockQuantity: 0m);

        dbContext.Manufacturers.AddRange(
            iekManufacturer,
            ekfManufacturer);

        dbContext.CharacteristicDefinitions.AddRange(
            seriesDefinition,
            currentDefinition,
            auxiliaryDefinition);

        dbContext.ProductTypes.AddRange(
            breakerType,
            switchType);

        dbContext.Products.AddRange(
            alpha,
            beta,
            gamma,
            delta,
            epsilon);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new ReaderCatalogData
        {
            IekManufacturer = iekManufacturer,
            EkfManufacturer = ekfManufacturer,
            BreakerType = breakerType,
            SwitchType = switchType,
            SeriesDefinition = seriesDefinition,
            CurrentDefinition = currentDefinition,
            AuxiliaryDefinition = auxiliaryDefinition,
            Alpha = alpha,
            Beta = beta,
            Gamma = gamma,
            Delta = delta,
            Epsilon = epsilon
        };
    }

    public static async Task<ReplacementCatalogData>
        CreateReplacementCatalogAsync(
            ElectronicDbContext dbContext,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        var suffix = Guid.NewGuid().ToString("N");

        var sourceManufacturer = CreateManufacturer(
            $"Source Brand {suffix}");

        var replacementManufacturer = CreateManufacturer(
            $"Replacement Brand {suffix}");

        var productType = TestDataFactory.CreateProductType(
            code: $"REPLACEMENT_BREAKER_{suffix}",
            name: $"Автомат для подбора {suffix}");

        var otherProductType = TestDataFactory.CreateProductType(
            code: $"OTHER_REPLACEMENT_TYPE_{suffix}",
            name: $"Другой тип {suffix}");

        var polesDefinition =
            TestDataFactory.CreateCharacteristicDefinition(
                code: $"POLES_{suffix}",
                name: $"Количество полюсов {suffix}",
                dataType: CharacteristicDataType.Number,
                unit: null);

        var currentDefinition =
            TestDataFactory.CreateCharacteristicDefinition(
                code: $"CURRENT_{suffix}",
                name: $"Номинальный ток {suffix}",
                dataType: CharacteristicDataType.Number,
                unit: "А");

        var ipDefinition =
            TestDataFactory.CreateCharacteristicDefinition(
                code: $"IP_{suffix}",
                name: $"Степень защиты {suffix}",
                dataType: CharacteristicDataType.Text,
                unit: null);

        var auxiliaryDefinition =
            TestDataFactory.CreateCharacteristicDefinition(
                code: $"AUXILIARY_{suffix}",
                name: $"Дополнительный контакт {suffix}",
                dataType: CharacteristicDataType.Boolean,
                unit: null);

        AddReplacementCharacteristic(
            productType,
            polesDefinition,
            ReplacementMatchMode.Exact,
            40);

        AddReplacementCharacteristic(
            productType,
            currentDefinition,
            ReplacementMatchMode.GreaterOrEqual,
            30);

        AddReplacementCharacteristic(
            productType,
            ipDefinition,
            ReplacementMatchMode.CompatibleOrHigher,
            20);

        AddReplacementCharacteristic(
            productType,
            auxiliaryDefinition,
            ReplacementMatchMode.Exact,
            10);

        var target = CreateProduct(
            article: $"TARGET-{suffix}",
            name: $"Target Breaker {suffix}",
            productType: productType,
            manufacturer: sourceManufacturer,
            price: 1_000m,
            stockQuantity: 0m);

        SetReplacementValues(
            target,
            productType,
            polesDefinition,
            currentDefinition,
            ipDefinition,
            auxiliaryDefinition,
            poles: 2m,
            current: 16m,
            ipRating: "IP20",
            hasAuxiliary: true);

        var perfectInStock = CreateProduct(
            article: $"PERFECT-IN-{suffix}",
            name: $"A Perfect Candidate {suffix}",
            productType: productType,
            manufacturer: replacementManufacturer,
            price: 1_100m,
            stockQuantity: 5m);

        SetReplacementValues(
            perfectInStock,
            productType,
            polesDefinition,
            currentDefinition,
            ipDefinition,
            auxiliaryDefinition,
            poles: 2m,
            current: 20m,
            ipRating: "IP44",
            hasAuxiliary: true);

        var perfectOutOfStock = CreateProduct(
            article: $"PERFECT-OUT-{suffix}",
            name: $"B Perfect Out Of Stock {suffix}",
            productType: productType,
            manufacturer: replacementManufacturer,
            price: 1_050m,
            stockQuantity: 0m);

        SetReplacementValues(
            perfectOutOfStock,
            productType,
            polesDefinition,
            currentDefinition,
            ipDefinition,
            auxiliaryDefinition,
            poles: 2m,
            current: 16m,
            ipRating: "IP54",
            hasAuxiliary: true);

        var partial = CreateProduct(
            article: $"PARTIAL-{suffix}",
            name: $"C Partial Candidate {suffix}",
            productType: productType,
            manufacturer: replacementManufacturer,
            price: 900m,
            stockQuantity: 3m);

        SetReplacementValues(
            partial,
            productType,
            polesDefinition,
            currentDefinition,
            ipDefinition,
            auxiliaryDefinition,
            poles: 2m,
            current: 10m,
            ipRating: "IP20",
            hasAuxiliary: false);

        var lowScore = CreateProduct(
            article: $"LOW-{suffix}",
            name: $"D Low Candidate {suffix}",
            productType: productType,
            manufacturer: replacementManufacturer,
            price: 800m,
            stockQuantity: 4m);

        SetReplacementValues(
            lowScore,
            productType,
            polesDefinition,
            currentDefinition,
            ipDefinition,
            auxiliaryDefinition,
            poles: 1m,
            current: 10m,
            ipRating: "IP10",
            hasAuxiliary: false);

        var otherTypeProduct = CreateProduct(
            article: $"OTHER-{suffix}",
            name: $"E Other Type Candidate {suffix}",
            productType: otherProductType,
            manufacturer: replacementManufacturer,
            price: 700m,
            stockQuantity: 8m);

        dbContext.Manufacturers.AddRange(
            sourceManufacturer,
            replacementManufacturer);

        dbContext.CharacteristicDefinitions.AddRange(
            polesDefinition,
            currentDefinition,
            ipDefinition,
            auxiliaryDefinition);

        dbContext.ProductTypes.AddRange(
            productType,
            otherProductType);

        dbContext.Products.AddRange(
            target,
            perfectInStock,
            perfectOutOfStock,
            partial,
            lowScore,
            otherTypeProduct);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new ReplacementCatalogData
        {
            Target = target,
            PerfectInStock = perfectInStock,
            PerfectOutOfStock = perfectOutOfStock,
            Partial = partial,
            LowScore = lowScore,
            OtherTypeProduct = otherTypeProduct
        };
    }

    private static Manufacturer CreateManufacturer(string name)
    {
        var result = Manufacturer.Create(name);

        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Не удалось создать производителя: " +
                $"{result.Error.Code}: {result.Error.Message}");
        }

        return result.Value;
    }

    private static Product CreateProduct(
        string article,
        string name,
        ProductType productType,
        Manufacturer manufacturer,
        decimal price,
        decimal stockQuantity)
    {
        return TestDataFactory.CreateProduct(
            article,
            name,
            productType.Id,
            manufacturer.Id,
            price,
            stockQuantity);
    }

    private static void AddFilterableCharacteristic(
        ProductType productType,
        CharacteristicDefinition definition)
    {
        TestDataFactory.AddCharacteristic(
            productType,
            definition,
            isRequired: false,
            isFilterable: true,
            isUsedForReplacement: false,
            replacementMatchMode: ReplacementMatchMode.None,
            replacementWeight: 0);
    }

    private static void AddReplacementCharacteristic(
        ProductType productType,
        CharacteristicDefinition definition,
        ReplacementMatchMode matchMode,
        int weight)
    {
        TestDataFactory.AddCharacteristic(
            productType,
            definition,
            isRequired: true,
            isFilterable: true,
            isUsedForReplacement: true,
            replacementMatchMode: matchMode,
            replacementWeight: weight);
    }

    private static void SetReplacementValues(
        Product product,
        ProductType productType,
        CharacteristicDefinition polesDefinition,
        CharacteristicDefinition currentDefinition,
        CharacteristicDefinition ipDefinition,
        CharacteristicDefinition auxiliaryDefinition,
        decimal poles,
        decimal current,
        string ipRating,
        bool hasAuxiliary)
    {
        SetCharacteristic(
            product,
            productType,
            polesDefinition,
            TestDataFactory.CreateNumberValue(poles));

        SetCharacteristic(
            product,
            productType,
            currentDefinition,
            TestDataFactory.CreateNumberValue(current));

        SetCharacteristic(
            product,
            productType,
            ipDefinition,
            TestDataFactory.CreateTextValue(ipRating));

        SetCharacteristic(
            product,
            productType,
            auxiliaryDefinition,
            TestDataFactory.CreateBooleanValue(hasAuxiliary));
    }

    private static void SetCharacteristic(
        Product product,
        ProductType productType,
        CharacteristicDefinition definition,
        ElectronicService.Domain.Catalog.ValueObjects.CharacteristicValue value)
    {
        var result = product.SetCharacteristic(
            productType,
            definition,
            value);

        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Не удалось установить характеристику: " +
                $"{result.Error.Code}: {result.Error.Message}");
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
                $"Не удалось добавить алиас: " +
                $"{result.Error.Code}: {result.Error.Message}");
        }
    }
}

internal sealed class ReaderCatalogData
{
    public required Manufacturer IekManufacturer { get; init; }

    public required Manufacturer EkfManufacturer { get; init; }

    public required ProductType BreakerType { get; init; }

    public required ProductType SwitchType { get; init; }

    public required CharacteristicDefinition SeriesDefinition { get; init; }

    public required CharacteristicDefinition CurrentDefinition { get; init; }

    public required CharacteristicDefinition AuxiliaryDefinition { get; init; }

    public required Product Alpha { get; init; }

    public required Product Beta { get; init; }

    public required Product Gamma { get; init; }

    public required Product Delta { get; init; }

    public required Product Epsilon { get; init; }
}

internal sealed class ReplacementCatalogData
{
    public required Product Target { get; init; }

    public required Product PerfectInStock { get; init; }

    public required Product PerfectOutOfStock { get; init; }

    public required Product Partial { get; init; }

    public required Product LowScore { get; init; }

    public required Product OtherTypeProduct { get; init; }
}
