using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.ProductTypes;
using ElectronicService.Infrastructure.Postgres.Data;
using ElectronicService.Domain.Catalog.Dictionaries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Seeding;

// это сервис, который будет вызываться при старте приложения, sealed означает, что от этого класса нельзя наследоваться
public sealed class CatalogDataSeeder
{
    
    // Список начальных терминов словаря, которые нужно добавить в БД, для распознования запросов пользователей, например, "ИЭК" -> "IEK"
    private static readonly CatalogDictionaryTermSeed[] DictionaryTerms =
    [
        new("ИЭК", CatalogDictionaryTermKind.Manufacturer, null, "IEK", 100),
        new("ИЕК", CatalogDictionaryTermKind.Manufacturer, null, "IEK", 100),
        new("IEK", CatalogDictionaryTermKind.Manufacturer, null, "IEK", 100),
        new("IEK ПРОМ СЕРИЯ", CatalogDictionaryTermKind.Manufacturer, null, "IEK", 100),

        new("ЕКФ", CatalogDictionaryTermKind.Manufacturer, null, "EKF", 100),
        new("EKF", CatalogDictionaryTermKind.Manufacturer, null, "EKF", 100),

        new("ЧИНТ", CatalogDictionaryTermKind.Manufacturer, null, "CHINT", 100),
        new("CHINT", CatalogDictionaryTermKind.Manufacturer, null, "CHINT", 100),
        new("CHINT ПРОМ СЕРИЯ", CatalogDictionaryTermKind.Manufacturer, null, "CHINT", 100),

        new("ABB", CatalogDictionaryTermKind.Manufacturer, null, "ABB", 100),
        new("АВВ", CatalogDictionaryTermKind.Manufacturer, null, "ABB", 100),
        new("АВВ ПРОМ СЕРИЯ", CatalogDictionaryTermKind.Manufacturer, null, "ABB", 100),

        new("DEKraft", CatalogDictionaryTermKind.Manufacturer, null, "DEKraft", 100),
        new("DEKraft пром серия", CatalogDictionaryTermKind.Manufacturer, null, "DEKraft", 100),

        new("КЭАЗ", CatalogDictionaryTermKind.Manufacturer, null, "КЭАЗ", 100),
        new("KEAZ", CatalogDictionaryTermKind.Manufacturer, null, "КЭАЗ", 100),

        new("ТДМ", CatalogDictionaryTermKind.Manufacturer, null, "ТДМ", 100),
        new("TDM", CatalogDictionaryTermKind.Manufacturer, null, "ТДМ", 100),

        new("DKC", CatalogDictionaryTermKind.Manufacturer, null, "DKC", 100),
        new("ДКС", CatalogDictionaryTermKind.Manufacturer, null, "DKC", 100),

        new("Legrand", CatalogDictionaryTermKind.Manufacturer, null, "Legrand", 100),
        new("Legrand пром серия", CatalogDictionaryTermKind.Manufacturer, null, "Legrand", 100),

        new("LSIS", CatalogDictionaryTermKind.Manufacturer, null, "LSIS", 100),
        new("LSIS ПРОМ СЕРИЯ", CatalogDictionaryTermKind.Manufacturer, null, "LSIS", 100),

        new("Schneider Electric", CatalogDictionaryTermKind.Manufacturer, null, "Schneider Electric", 100),
        new("Шнайдер", CatalogDictionaryTermKind.Manufacturer, null, "Schneider Electric", 100),
        new("Шнайдер CVS", CatalogDictionaryTermKind.Manufacturer, null, "Schneider Electric", 100),
        new("Шнайдер EZC", CatalogDictionaryTermKind.Manufacturer, null, "Schneider Electric", 100),
        new("Шнайдер пром", CatalogDictionaryTermKind.Manufacturer, null, "Schneider Electric", 100),

        new("Systeme El", CatalogDictionaryTermKind.Manufacturer, null, "Systeme El", 100),
        new("Systeme Electric", CatalogDictionaryTermKind.Manufacturer, null, "Systeme El", 100),

        new("C&S Electric", CatalogDictionaryTermKind.Manufacturer, null, "C&S Electric", 100),
        new("C&S", CatalogDictionaryTermKind.Manufacturer, null, "C&S Electric", 100),

        new("Hyundai", CatalogDictionaryTermKind.Manufacturer, null, "Hyundai", 100),

        new("АВТОМАТ", CatalogDictionaryTermKind.ProductType, null, "MODULAR_CIRCUIT_BREAKER", 50),
        new("АВТОМАТИЧЕСКИЙ ВЫКЛЮЧАТЕЛЬ", CatalogDictionaryTermKind.ProductType, null, "MODULAR_CIRCUIT_BREAKER", 100),

        new("РУБИЛЬНИК", CatalogDictionaryTermKind.ProductType, null, "SWITCH_DISCONNECTOR", 100),
        new("ВЫКЛЮЧАТЕЛЬ НАГРУЗКИ", CatalogDictionaryTermKind.ProductType, null, "LOAD_SWITCH", 100),

        new("АРМАТ", CatalogDictionaryTermKind.Characteristic, "PRODUCT_SERIES", "ARMAT", 100),
        new("ARMAT", CatalogDictionaryTermKind.Characteristic, "PRODUCT_SERIES", "ARMAT", 100),

        new("ПРОКСИМА", CatalogDictionaryTermKind.Characteristic, "PRODUCT_SERIES", "PROXIMA", 100),
        new("PROXIMA", CatalogDictionaryTermKind.Characteristic, "PRODUCT_SERIES", "PROXIMA", 100),

        new("ОДНОПОЛЮСНЫЙ", CatalogDictionaryTermKind.Characteristic, "POLES", "1", 100),
        new("ДВУХПОЛЮСНЫЙ", CatalogDictionaryTermKind.Characteristic, "POLES", "2", 100),
        new("ТРЕХПОЛЮСНЫЙ", CatalogDictionaryTermKind.Characteristic, "POLES", "3", 100),
        new("ЧЕТЫРЕХПОЛЮСНЫЙ", CatalogDictionaryTermKind.Characteristic, "POLES", "4", 100),

        new("РЕВЕРСИВНЫЙ", CatalogDictionaryTermKind.Characteristic, "REVERSIBLE", "TRUE", 100),
        new("С ЗАЩИТОЙ", CatalogDictionaryTermKind.Characteristic, "HAS_THERMAL_RELEASE", "TRUE", 100),
        new("БЕЗ ЗАЩИТЫ", CatalogDictionaryTermKind.Characteristic, "HAS_THERMAL_RELEASE", "FALSE", 100)
    ];

    // Это список типов товаров, которые нужно добавить в БД
    private static readonly CatalogProductTypeSeed[] ProductTypes =
    [
        new("MODULAR_CIRCUIT_BREAKER", "Модульный автомат"),
        new("DIFFERENTIAL_CIRCUIT_BREAKER", "Дифференциальный автомат"),
        new("RCD", "УЗО"),
        new("POWER_CIRCUIT_BREAKER", "Силовой автомат"),

        new("FLOOR_STANDING_CABINET", "Напольный корпус"),
        new("SCHMP_CABINET", "Корпус ЩМП"),
        new("MODULAR_DISTRIBUTION_CABINET", "Корпус ЩРн/ЩРв"),

        new("LOAD_SWITCH", "Выключатель нагрузки"),
        new("SWITCH_DISCONNECTOR", "Рубильник")
    ];

    private static readonly CatalogCharacteristicSeed[] Characteristics =
    [
        // Общие
        new("PRODUCT_SERIES", "Серия товара", CharacteristicDataType.Text, null),

        // Автоматы, УЗО, дифавтоматы
        new("RATED_CURRENT", "Номинальный ток", CharacteristicDataType.Number, "А"),
        new("POLES", "Количество полюсов", CharacteristicDataType.Number, null),
        new("MODULE_COUNT", "Количество модулей", CharacteristicDataType.Number, null),
        new("CURVE", "Характеристика срабатывания", CharacteristicDataType.Text, null),
        new("BREAKING_CAPACITY", "ПКС", CharacteristicDataType.Number, "кА"),
        new("LEAKAGE_CURRENT", "Ток утечки", CharacteristicDataType.Number, "мА"),
        new("DIFFERENTIAL_CURRENT_TYPE", "Тип дифференциального тока", CharacteristicDataType.Text, null),
        new("HAS_THERMAL_RELEASE", "Наличие теплового расцепителя", CharacteristicDataType.Boolean, null),
        new("RELEASE_TYPE", "Тип расцепителя", CharacteristicDataType.Text, null),

        // Рубильники / выключатели нагрузки
        new("REVERSIBLE", "Реверсивный", CharacteristicDataType.Boolean, null),

        // Корпуса
        new("CABINET_KIND", "Вид корпуса", CharacteristicDataType.Text, null),
        new("HEIGHT", "Высота", CharacteristicDataType.Number, "мм"),
        new("WIDTH", "Ширина", CharacteristicDataType.Number, "мм"),
        new("DEPTH", "Глубина", CharacteristicDataType.Number, "мм"),
        new("IP_RATING", "Степень защиты", CharacteristicDataType.Text, null),
        new("HAS_METER_SECTION", "Наличие отсека под счётчик", CharacteristicDataType.Boolean, null),
        new("MATERIAL", "Материал корпуса", CharacteristicDataType.Text, null),
        new("MOUNTING_METHOD", "Способ установки", CharacteristicDataType.Text, null),
        new("EXECUTION_TYPE", "Исполнение", CharacteristicDataType.Text, null)
    ];

    private static readonly CatalogProductTypeCharacteristicSeed[] ProductTypeCharacteristics =
    [
        // =========================
        // Модульные автоматы
        // =========================
        new(
            "MODULAR_CIRCUIT_BREAKER",
            "RATED_CURRENT",
            IsRequired: true,
            IsFilterable: true,
            IsUsedForReplacement: true,
            ReplacementMatchMode.Exact,
            ReplacementWeight: 100),

        new(
            "MODULAR_CIRCUIT_BREAKER",
            "POLES",
            IsRequired: true,
            IsFilterable: true,
            IsUsedForReplacement: true,
            ReplacementMatchMode.Exact,
            ReplacementWeight: 100),

        new(
            "MODULAR_CIRCUIT_BREAKER",
            "CURVE",
            IsRequired: true,
            IsFilterable: true,
            IsUsedForReplacement: true,
            ReplacementMatchMode.Exact,
            ReplacementWeight: 90),

        new(
            "MODULAR_CIRCUIT_BREAKER",
            "BREAKING_CAPACITY",
            IsRequired: true,
            IsFilterable: true,
            IsUsedForReplacement: true,
            ReplacementMatchMode.GreaterOrEqual,
            ReplacementWeight: 80),

        new(
            "MODULAR_CIRCUIT_BREAKER",
            "HAS_THERMAL_RELEASE",
            IsRequired: false,
            IsFilterable: true,
            IsUsedForReplacement: false,
            ReplacementMatchMode.None,
            ReplacementWeight: 0),

        new(
            "MODULAR_CIRCUIT_BREAKER",
            "PRODUCT_SERIES",
            IsRequired: false,
            IsFilterable: true,
            IsUsedForReplacement: false,
            ReplacementMatchMode.None,
            ReplacementWeight: 0),

        // =========================
        // Дифференциальные автоматы
        // =========================
        new(
            "DIFFERENTIAL_CIRCUIT_BREAKER",
            "RATED_CURRENT",
            IsRequired: true,
            IsFilterable: true,
            IsUsedForReplacement: true,
            ReplacementMatchMode.Exact,
            ReplacementWeight: 100),

        new(
            "DIFFERENTIAL_CIRCUIT_BREAKER",
            "POLES",
            IsRequired: true,
            IsFilterable: true,
            IsUsedForReplacement: true,
            ReplacementMatchMode.Exact,
            ReplacementWeight: 100),

        new(
            "DIFFERENTIAL_CIRCUIT_BREAKER",
            "CURVE",
            IsRequired: true,
            IsFilterable: true,
            IsUsedForReplacement: true,
            ReplacementMatchMode.Exact,
            ReplacementWeight: 90),

        new(
            "DIFFERENTIAL_CIRCUIT_BREAKER",
            "LEAKAGE_CURRENT",
            IsRequired: true,
            IsFilterable: true,
            IsUsedForReplacement: true,
            ReplacementMatchMode.Exact,
            ReplacementWeight: 100),

        new(
            "DIFFERENTIAL_CIRCUIT_BREAKER",
            "DIFFERENTIAL_CURRENT_TYPE",
            IsRequired: false,
            IsFilterable: true,
            IsUsedForReplacement: true,
            ReplacementMatchMode.Exact,
            ReplacementWeight: 70),

        new(
            "DIFFERENTIAL_CIRCUIT_BREAKER",
            "BREAKING_CAPACITY",
            IsRequired: true,
            IsFilterable: true,
            IsUsedForReplacement: true,
            ReplacementMatchMode.GreaterOrEqual,
            ReplacementWeight: 80),

        new(
            "DIFFERENTIAL_CIRCUIT_BREAKER",
            "PRODUCT_SERIES",
            IsRequired: false,
            IsFilterable: true,
            IsUsedForReplacement: false,
            ReplacementMatchMode.None,
            ReplacementWeight: 0),

        // =========================
        // УЗО
        // =========================
        new(
            "RCD",
            "RATED_CURRENT",
            IsRequired: true,
            IsFilterable: true,
            IsUsedForReplacement: true,
            ReplacementMatchMode.Exact,
            ReplacementWeight: 100),

        new(
            "RCD",
            "POLES",
            IsRequired: true,
            IsFilterable: true,
            IsUsedForReplacement: true,
            ReplacementMatchMode.Exact,
            ReplacementWeight: 100),

        new(
            "RCD",
            "LEAKAGE_CURRENT",
            IsRequired: true,
            IsFilterable: true,
            IsUsedForReplacement: true,
            ReplacementMatchMode.Exact,
            ReplacementWeight: 100),

        new(
            "RCD",
            "DIFFERENTIAL_CURRENT_TYPE",
            IsRequired: false,
            IsFilterable: true,
            IsUsedForReplacement: true,
            ReplacementMatchMode.Exact,
            ReplacementWeight: 70),

        new(
            "RCD",
            "BREAKING_CAPACITY",
            IsRequired: false,
            IsFilterable: true,
            IsUsedForReplacement: true,
            ReplacementMatchMode.GreaterOrEqual,
            ReplacementWeight: 50),

        // =========================
        // Силовые автоматы
        // =========================
        new(
            "POWER_CIRCUIT_BREAKER",
            "RATED_CURRENT",
            IsRequired: true,
            IsFilterable: true,
            IsUsedForReplacement: true,
            ReplacementMatchMode.Exact,
            ReplacementWeight: 100),

        new(
            "POWER_CIRCUIT_BREAKER",
            "POLES",
            IsRequired: true,
            IsFilterable: true,
            IsUsedForReplacement: true,
            ReplacementMatchMode.Exact,
            ReplacementWeight: 100),

        new(
            "POWER_CIRCUIT_BREAKER",
            "RELEASE_TYPE",
            IsRequired: false,
            IsFilterable: true,
            IsUsedForReplacement: true,
            ReplacementMatchMode.Exact,
            ReplacementWeight: 80),

        new(
            "POWER_CIRCUIT_BREAKER",
            "BREAKING_CAPACITY",
            IsRequired: false,
            IsFilterable: true,
            IsUsedForReplacement: true,
            ReplacementMatchMode.GreaterOrEqual,
            ReplacementWeight: 70),

        new(
            "POWER_CIRCUIT_BREAKER",
            "PRODUCT_SERIES",
            IsRequired: false,
            IsFilterable: true,
            IsUsedForReplacement: false,
            ReplacementMatchMode.None,
            ReplacementWeight: 0),

        // =========================
        // Напольные корпуса: ВРУ / КЛ / ШНС / ЩО-70
        // =========================
        new(
            "FLOOR_STANDING_CABINET",
            "CABINET_KIND",
            IsRequired: true,
            IsFilterable: true,
            IsUsedForReplacement: true,
            ReplacementMatchMode.Exact,
            ReplacementWeight: 100),

        new(
            "FLOOR_STANDING_CABINET",
            "HEIGHT",
            IsRequired: true,
            IsFilterable: true,
            IsUsedForReplacement: true,
            ReplacementMatchMode.Near,
            ReplacementWeight: 90),

        new(
            "FLOOR_STANDING_CABINET",
            "WIDTH",
            IsRequired: true,
            IsFilterable: true,
            IsUsedForReplacement: true,
            ReplacementMatchMode.Near,
            ReplacementWeight: 90),

        new(
            "FLOOR_STANDING_CABINET",
            "DEPTH",
            IsRequired: true,
            IsFilterable: true,
            IsUsedForReplacement: true,
            ReplacementMatchMode.Near,
            ReplacementWeight: 80),

        new(
            "FLOOR_STANDING_CABINET",
            "IP_RATING",
            IsRequired: false,
            IsFilterable: true,
            IsUsedForReplacement: true,
            ReplacementMatchMode.CompatibleOrHigher,
            ReplacementWeight: 70),

        new(
            "FLOOR_STANDING_CABINET",
            "HAS_METER_SECTION",
            IsRequired: false,
            IsFilterable: true,
            IsUsedForReplacement: true,
            ReplacementMatchMode.Exact,
            ReplacementWeight: 60),

        // =========================
        // Корпуса ЩМП
        // =========================
        new(
            "SCHMP_CABINET",
            "CABINET_KIND",
            IsRequired: true,
            IsFilterable: true,
            IsUsedForReplacement: true,
            ReplacementMatchMode.Exact,
            ReplacementWeight: 100),

        new(
            "SCHMP_CABINET",
            "HEIGHT",
            IsRequired: true,
            IsFilterable: true,
            IsUsedForReplacement: true,
            ReplacementMatchMode.Near,
            ReplacementWeight: 90),

        new(
            "SCHMP_CABINET",
            "WIDTH",
            IsRequired: true,
            IsFilterable: true,
            IsUsedForReplacement: true,
            ReplacementMatchMode.Near,
            ReplacementWeight: 90),

        new(
            "SCHMP_CABINET",
            "DEPTH",
            IsRequired: true,
            IsFilterable: true,
            IsUsedForReplacement: true,
            ReplacementMatchMode.Near,
            ReplacementWeight: 80),

        new(
            "SCHMP_CABINET",
            "IP_RATING",
            IsRequired: false,
            IsFilterable: true,
            IsUsedForReplacement: true,
            ReplacementMatchMode.CompatibleOrHigher,
            ReplacementWeight: 70),

        new(
            "SCHMP_CABINET",
            "MATERIAL",
            IsRequired: false,
            IsFilterable: true,
            IsUsedForReplacement: true,
            ReplacementMatchMode.Exact,
            ReplacementWeight: 50),

        new(
            "SCHMP_CABINET",
            "MOUNTING_METHOD",
            IsRequired: false,
            IsFilterable: true,
            IsUsedForReplacement: true,
            ReplacementMatchMode.Exact,
            ReplacementWeight: 60),

        new(
            "SCHMP_CABINET",
            "EXECUTION_TYPE",
            IsRequired: false,
            IsFilterable: true,
            IsUsedForReplacement: false,
            ReplacementMatchMode.None,
            ReplacementWeight: 0),

        new(
            "SCHMP_CABINET",
            "PRODUCT_SERIES",
            IsRequired: false,
            IsFilterable: true,
            IsUsedForReplacement: false,
            ReplacementMatchMode.None,
            ReplacementWeight: 0),

        // =========================
        // Корпуса ЩРн / ЩРв
        // =========================
        new(
            "MODULAR_DISTRIBUTION_CABINET",
            "CABINET_KIND",
            IsRequired: true,
            IsFilterable: true,
            IsUsedForReplacement: true,
            ReplacementMatchMode.Exact,
            ReplacementWeight: 100),

        new(
            "MODULAR_DISTRIBUTION_CABINET",
            "MODULE_COUNT",
            IsRequired: true,
            IsFilterable: true,
            IsUsedForReplacement: true,
            ReplacementMatchMode.Exact,
            ReplacementWeight: 100),

        new(
            "MODULAR_DISTRIBUTION_CABINET",
            "MOUNTING_METHOD",
            IsRequired: true,
            IsFilterable: true,
            IsUsedForReplacement: true,
            ReplacementMatchMode.Exact,
            ReplacementWeight: 90),

        new(
            "MODULAR_DISTRIBUTION_CABINET",
            "IP_RATING",
            IsRequired: false,
            IsFilterable: true,
            IsUsedForReplacement: true,
            ReplacementMatchMode.CompatibleOrHigher,
            ReplacementWeight: 70),

        new(
            "MODULAR_DISTRIBUTION_CABINET",
            "MATERIAL",
            IsRequired: false,
            IsFilterable: true,
            IsUsedForReplacement: true,
            ReplacementMatchMode.Exact,
            ReplacementWeight: 50),

        new(
            "MODULAR_DISTRIBUTION_CABINET",
            "EXECUTION_TYPE",
            IsRequired: false,
            IsFilterable: true,
            IsUsedForReplacement: false,
            ReplacementMatchMode.None,
            ReplacementWeight: 0),

        // =========================
        // Выключатели нагрузки
        // =========================
        new(
            "LOAD_SWITCH",
            "RATED_CURRENT",
            IsRequired: true,
            IsFilterable: true,
            IsUsedForReplacement: true,
            ReplacementMatchMode.Exact,
            ReplacementWeight: 100),

        new(
            "LOAD_SWITCH",
            "POLES",
            IsRequired: true,
            IsFilterable: true,
            IsUsedForReplacement: true,
            ReplacementMatchMode.Exact,
            ReplacementWeight: 100),

        new(
            "LOAD_SWITCH",
            "MODULE_COUNT",
            IsRequired: false,
            IsFilterable: true,
            IsUsedForReplacement: true,
            ReplacementMatchMode.Exact,
            ReplacementWeight: 60),

        new(
            "LOAD_SWITCH",
            "MOUNTING_METHOD",
            IsRequired: false,
            IsFilterable: true,
            IsUsedForReplacement: true,
            ReplacementMatchMode.Exact,
            ReplacementWeight: 60),

        new(
            "LOAD_SWITCH",
            "PRODUCT_SERIES",
            IsRequired: false,
            IsFilterable: true,
            IsUsedForReplacement: false,
            ReplacementMatchMode.None,
            ReplacementWeight: 0),

        // =========================
        // Рубильники
        // =========================
        new(
            "SWITCH_DISCONNECTOR",
            "RATED_CURRENT",
            IsRequired: true,
            IsFilterable: true,
            IsUsedForReplacement: true,
            ReplacementMatchMode.Exact,
            ReplacementWeight: 100),

        new(
            "SWITCH_DISCONNECTOR",
            "POLES",
            IsRequired: true,
            IsFilterable: true,
            IsUsedForReplacement: true,
            ReplacementMatchMode.Exact,
            ReplacementWeight: 100),

        new(
            "SWITCH_DISCONNECTOR",
            "REVERSIBLE",
            IsRequired: false,
            IsFilterable: true,
            IsUsedForReplacement: true,
            ReplacementMatchMode.Exact,
            ReplacementWeight: 70),

        new(
            "SWITCH_DISCONNECTOR",
            "MOUNTING_METHOD",
            IsRequired: false,
            IsFilterable: true,
            IsUsedForReplacement: true,
            ReplacementMatchMode.Exact,
            ReplacementWeight: 60),

        new(
            "SWITCH_DISCONNECTOR",
            "PRODUCT_SERIES",
            IsRequired: false,
            IsFilterable: true,
            IsUsedForReplacement: false,
            ReplacementMatchMode.None,
            ReplacementWeight: 0)
    ];

    private static readonly Action<ILogger, Exception?> CatalogSeedCompleted =
    LoggerMessage.Define(
        LogLevel.Information,
        new EventId(1, nameof(CatalogSeedCompleted)),
        "Catalog seed completed.");

    private readonly ElectronicDbContext _dbContext;
    private readonly ILogger<CatalogDataSeeder> _logger;

    public CatalogDataSeeder(
        ElectronicDbContext dbContext,
        ILogger<CatalogDataSeeder> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedCharacteristicsAsync(cancellationToken).ConfigureAwait(false);

        await SeedProductTypesAsync(cancellationToken).ConfigureAwait(false);

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await SeedProductTypeCharacteristicsAsync(cancellationToken).ConfigureAwait(false);

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await SeedDictionaryTermsAsync(cancellationToken).ConfigureAwait(false);

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        CatalogSeedCompleted(_logger, null);
    }

    // Этот метод добавляет отсутствующие характеристики
    private async Task SeedCharacteristicsAsync(CancellationToken cancellationToken)
    {
        var existingCodes = await _dbContext.CharacteristicDefinitions
            .Select(characteristic => characteristic.Code)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var existingCodeSet = existingCodes.ToHashSet(StringComparer.Ordinal);

        var missingCharacteristics = Characteristics
            .Where(characteristic => !existingCodeSet.Contains(characteristic.Code))
            .ToArray();

        foreach (var seed in missingCharacteristics)
        {
            var characteristicResult = CharacteristicDefinition.Create(
                seed.Code,
                seed.Name,
                seed.DataType,
                seed.Unit);

            if (characteristicResult.IsFailure)
            {
                throw new InvalidOperationException(characteristicResult.Error.Message);
            }

            await _dbContext.CharacteristicDefinitions
                .AddAsync(characteristicResult.Value, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    // Этот метод почти такой же, как SeedCharacteristicsAsync, только для типов товаров.
    /*
    Что он делает:
    Берёт из БД существующие ProductType.Code.
    Сравнивает с массивом ProductTypes.
    Находит отсутствующие типы товаров.
    Создаёт их через ProductType.Create.
    Добавляет в DbContext.
    */
    private async Task SeedProductTypesAsync(CancellationToken cancellationToken)
    {
        var existingCodes = await _dbContext.ProductTypes
            .Select(productType => productType.Code)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var existingCodeSet = existingCodes.ToHashSet(StringComparer.Ordinal);

        var missingProductTypes = ProductTypes
            .Where(productType => !existingCodeSet.Contains(productType.Code))
            .ToArray();

        foreach (var seed in missingProductTypes)
        {
            var productTypeResult = ProductType.Create(seed.Code, seed.Name);

            if (productTypeResult.IsFailure)
            {
                throw new InvalidOperationException(productTypeResult.Error.Message);
            }

            await _dbContext.ProductTypes
                .AddAsync(productTypeResult.Value, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    // Он связывает типы товаров с характеристиками, самый важный метод в этом классе, потому что он создаёт связи между типами товаров и характеристиками.
    // Самый сложный метод
    private async Task SeedProductTypeCharacteristicsAsync(CancellationToken cancellationToken)
    {
        // Загружаем типы товаров вместе с уже добавленными характеристиками
        // Include нужен, чтобы загрузить коллекцию
        var productTypes = await _dbContext.ProductTypes
            .Include(productType => productType.Characteristics)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Загружаем все характеристики
        // Нам нужны сами объекты CharacteristicDefinition, потому что ProductType.AddCharacteristic(...) принимает не просто Guid, а доменную сущность
        var characteristics = await _dbContext.CharacteristicDefinitions
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Проходим по каждой связи из seed-массива
        foreach (var seed in ProductTypeCharacteristics)
        {
            // Ищем тип товара по коду
            var productType = productTypes.FirstOrDefault(productType =>
                string.Equals(
                    productType.Code,
                    seed.ProductTypeCode,
                    StringComparison.Ordinal));

            if (productType is null)
            {
                throw new InvalidOperationException(
                    $"Product type '{seed.ProductTypeCode}' was not found.");
            }

            var characteristic = characteristics.FirstOrDefault(characteristic =>
                string.Equals(
                    characteristic.Code,
                    seed.CharacteristicCode,
                    StringComparison.Ordinal));

            if (characteristic is null)
            {
                throw new InvalidOperationException(
                    $"Characteristic '{seed.CharacteristicCode}' was not found.");
            }

            if (productType.AllowsCharacteristic(characteristic.Id))
            {
                continue;
            }

            var addResult = productType.AddCharacteristic(
                characteristic,
                seed.IsRequired,
                seed.IsFilterable,
                seed.IsUsedForReplacement,
                seed.ReplacementMatchMode,
                seed.ReplacementWeight);

            if (addResult.IsFailure)
            {
                throw new InvalidOperationException(addResult.Error.Message);
            }
        }
    }

    private async Task SeedDictionaryTermsAsync(CancellationToken cancellationToken)
    {
        var existingTerms = await _dbContext.CatalogDictionaryTerms
            .Select(term => new
            {
                term.NormalizedPhrase,
                term.Kind,
                term.TargetCode,
                term.TargetValue
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var existingKeys = existingTerms
            .Select(term => CreateDictionaryTermKey(
                term.NormalizedPhrase,
                term.Kind,
                term.TargetCode,
                term.TargetValue))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var seed in DictionaryTerms)
        {
            var termResult = CatalogDictionaryTerm.Create(
                seed.Phrase,
                seed.Kind,
                seed.TargetCode,
                seed.TargetValue,
                seed.Priority,
                CatalogDictionaryTermStatus.Approved,
                CatalogDictionaryTermSource.Seed);

            if (termResult.IsFailure)
            {
                continue;
            }

            var term = termResult.Value;

            var key = CreateDictionaryTermKey(
                term.NormalizedPhrase,
                term.Kind,
                term.TargetCode,
                term.TargetValue);

            if (!existingKeys.Add(key))
            {
                continue;
            }

            _dbContext.CatalogDictionaryTerms.Add(term);
        }
    }

    private static string CreateDictionaryTermKey(
        string normalizedPhrase,
        CatalogDictionaryTermKind kind,
        string? targetCode,
        string targetValue)
    {
        return string.Join(
            "|",
            normalizedPhrase,
            kind.ToString(),
            targetCode ?? string.Empty,
            targetValue);
    }
}