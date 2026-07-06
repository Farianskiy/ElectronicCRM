using System.Globalization;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Import;

internal static class ExcelImportProfileResolver
{
    public static ExcelImportProfile Resolve(string fileName)
    {
        var normalizedFileName = fileName.ToUpperInvariant();

        if (normalizedFileName.Contains("МОДУЛЬНЫЕ АВТОМАТЫ", StringComparison.Ordinal))
        {
            return ModularCircuitBreaker();
        }

        if (normalizedFileName.Contains("ДИФ", StringComparison.Ordinal))
        {
            return DifferentialCircuitBreaker();
        }

        if (normalizedFileName.Contains("УЗО", StringComparison.Ordinal))
        {
            return Rcd();
        }

        if (normalizedFileName.Contains("СИЛОВЫЕ", StringComparison.Ordinal))
        {
            return PowerCircuitBreaker();
        }

        if (normalizedFileName.Contains("ВЫКЛЮЧ", StringComparison.Ordinal)
            && normalizedFileName.Contains("НАГРУЗ", StringComparison.Ordinal))
        {
            return LoadSwitch();
        }

        if (normalizedFileName.Contains("ВРУ", StringComparison.Ordinal))
        {
            return FloorStandingCabinet("ВРУ", new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Высота ВРУ"] = "HEIGHT",
                ["Ширина ВРУ"] = "WIDTH",
                ["Глубина ВРУ"] = "DEPTH",
                ["Степень защиты ВРУ"] = "IP_RATING",
                ["Наличие отсека под счетчик"] = "HAS_METER_SECTION"
            }, "Производитель ВРУ");
        }

        if (normalizedFileName.Contains("КОРПУСА КЛ", StringComparison.Ordinal)
            || normalizedFileName.Contains("КОРПУС КЛ", StringComparison.Ordinal))
        {
            return FloorStandingCabinet("КЛ", new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Высота КЛ"] = "HEIGHT",
                ["Ширина КЛ"] = "WIDTH",
                ["Глубина КЛ"] = "DEPTH",
                ["Степень защиты"] = "IP_RATING"
            }, "Производитель КЛ");
        }

        if (normalizedFileName.Contains("ШНС", StringComparison.Ordinal))
        {
            return FloorStandingCabinet("ШНС", new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Высота корпуса ШНС (Корпуса ШНС)"] = "HEIGHT",
                ["Ширина корпуса ШНС (Корпуса ШНС)"] = "WIDTH",
                ["Глубина корпуса ШНС (Корпуса ШНС)"] = "DEPTH",
                ["Степень защиты корп.ШНС (Корпуса ШНС)"] = "IP_RATING"
            }, "Производитель корп.ШНС (Корпуса ШНС)");
        }

        if (normalizedFileName.Contains("ЩО70", StringComparison.Ordinal)
            || normalizedFileName.Contains("ЩО-70", StringComparison.Ordinal))
        {
            return FloorStandingCabinet("ЩО-70", new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Высота корпуса"] = "HEIGHT",
                ["Ширина корпуса"] = "WIDTH",
                ["Глубина корпус"] = "DEPTH",
                ["Наличие отсека под счетчики"] = "HAS_METER_SECTION"
            }, "Производитель");
        }

        if (normalizedFileName.Contains("ЩМП", StringComparison.Ordinal))
        {
            return SchmpCabinet();
        }

        if (normalizedFileName.Contains("ЩРН", StringComparison.Ordinal)
            || normalizedFileName.Contains("ЩРВ", StringComparison.Ordinal)
            || normalizedFileName.Contains("ЩРН ЩРВ", StringComparison.Ordinal))
        {
            return ModularDistributionCabinet();
        }

        if (normalizedFileName.Contains("ВР32", StringComparison.Ordinal))
        {
            return SwitchDisconnector("ВР32");
        }

        if (normalizedFileName.Contains("РЕ19", StringComparison.Ordinal))
        {
            return SwitchDisconnector("РЕ19");
        }

        if (normalizedFileName.Contains("РУБИЛЬНИК", StringComparison.Ordinal))
        {
            return SwitchDisconnector(null);
        }

        if (normalizedFileName.Contains("ВЫКЛЮЧ", StringComparison.Ordinal)
            && normalizedFileName.Contains("НАГРУЗ", StringComparison.Ordinal))
        {
            return LoadSwitch();
        }

        throw new InvalidOperationException(
            string.Create(
                CultureInfo.InvariantCulture,
                $"Import profile for file '{fileName}' was not found."));
    }

    private static ExcelImportProfile ModularCircuitBreaker()
    {
        return new ExcelImportProfile(
            ProductTypeCode: "MODULAR_CIRCUIT_BREAKER",
            DefaultCabinetKind: null,
            DefaultProductSeries: null,
            NameColumn: "Наименование",
            ArticleColumn: null,
            CodeColumn: null,
            ManufacturerColumn: "Производитель автоматы",
            CharacteristicColumnMap: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Количество полюсов"] = "POLES",
                ["Номинальный ток"] = "RATED_CURRENT",
                ["Характеристика срабатывания"] = "CURVE",
                ["ПКС"] = "BREAKING_CAPACITY",
                ["Наличие теплового рсцеп"] = "HAS_THERMAL_RELEASE",
                ["Производитель Серия"] = "PRODUCT_SERIES"
            });
    }

    private static ExcelImportProfile DifferentialCircuitBreaker()
    {
        return new ExcelImportProfile(
            ProductTypeCode: "DIFFERENTIAL_CIRCUIT_BREAKER",
            DefaultCabinetKind: null,
            DefaultProductSeries: null,
            NameColumn: "Наименование",
            ArticleColumn: null,
            CodeColumn: null,
            ManufacturerColumn: "Производитель автоматы",
            CharacteristicColumnMap: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Количество полюсов"] = "POLES",
                ["Номинальный ток"] = "RATED_CURRENT",
                ["Характеристика срабатывания"] = "CURVE",
                ["Ток утечки"] = "LEAKAGE_CURRENT",
                ["ПКС"] = "BREAKING_CAPACITY",
                ["Характеристика срабатывания по диф.току"] = "DIFFERENTIAL_CURRENT_TYPE",
                ["Производитель Серия"] = "PRODUCT_SERIES"
            });
    }

    private static ExcelImportProfile Rcd()
    {
        return new ExcelImportProfile(
            ProductTypeCode: "RCD",
            DefaultCabinetKind: null,
            DefaultProductSeries: null,
            NameColumn: "Наименование",
            ArticleColumn: "Артикул",
            CodeColumn: "Код",
            ManufacturerColumn: "Производитель автоматы (УЗО)",
            CharacteristicColumnMap: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Количество полюсов (УЗО)"] = "POLES",
                ["Номинальный ток (УЗО)"] = "RATED_CURRENT",
                ["ПКС (УЗО)"] = "BREAKING_CAPACITY",
                ["Ток утечки (УЗО)"] = "LEAKAGE_CURRENT",
                ["Тип рабочей характеристики (УЗО)"] = "DIFFERENTIAL_CURRENT_TYPE"
            });
    }

    private static ExcelImportProfile PowerCircuitBreaker()
    {
        return new ExcelImportProfile(
            ProductTypeCode: "POWER_CIRCUIT_BREAKER",
            DefaultCabinetKind: null,
            DefaultProductSeries: null,
            NameColumn: "Наименование",
            ArticleColumn: null,
            CodeColumn: null,
            ManufacturerColumn: "Производитель автоматы",
            CharacteristicColumnMap: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Количество полюсов"] = "POLES",
                ["Номинальный ток"] = "RATED_CURRENT",
                ["Тип расцепителя"] = "RELEASE_TYPE",
                ["ПКС"] = "BREAKING_CAPACITY",
                ["Производитель Серия"] = "PRODUCT_SERIES"
            });
    }

    private static ExcelImportProfile FloorStandingCabinet(
        string cabinetKind,
        IReadOnlyDictionary<string, string> map,
        string manufacturerColumn)
    {
        return new ExcelImportProfile(
            ProductTypeCode: "FLOOR_STANDING_CABINET",
            DefaultCabinetKind: cabinetKind,
            DefaultProductSeries: null,
            NameColumn: "Наименование",
            ArticleColumn: null,
            CodeColumn: null,
            ManufacturerColumn: manufacturerColumn,
            CharacteristicColumnMap: map);
    }

    private static ExcelImportProfile SchmpCabinet()
    {
        return new ExcelImportProfile(
            ProductTypeCode: "SCHMP_CABINET",
            DefaultCabinetKind: "ЩМП",
            DefaultProductSeries: null,
            NameColumn: "Наименование",
            ArticleColumn: null,
            CodeColumn: null,
            ManufacturerColumn: "Производитель (ЩМП)",
            CharacteristicColumnMap: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Высота (ЩМП)"] = "HEIGHT",
                ["Ширина (ЩМП)"] = "WIDTH",
                ["Глубина (ЩМП)"] = "DEPTH",
                ["Степень защиты (ЩМП)"] = "IP_RATING",
                ["Материал корпуса (ЩМП)"] = "MATERIAL",
                ["Исполнение (ЩМП)"] = "EXECUTION_TYPE",
                ["Производитель серия (ЩМП)"] = "PRODUCT_SERIES"
            });
    }

    private static ExcelImportProfile ModularDistributionCabinet()
    {
        return new ExcelImportProfile(
            ProductTypeCode: "MODULAR_DISTRIBUTION_CABINET",
            DefaultCabinetKind: null,
            DefaultProductSeries: null,
            NameColumn: "Наименование",
            ArticleColumn: null,
            CodeColumn: null,
            ManufacturerColumn: "Производитель ЩРн",
            CharacteristicColumnMap: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Кол-во модулей"] = "MODULE_COUNT",
                ["Способ установки"] = "MOUNTING_METHOD",
                ["Материал корпуса"] = "MATERIAL",
                ["Степень защиты"] = "IP_RATING",
                ["Исполнение"] = "EXECUTION_TYPE"
            });
    }

    private static ExcelImportProfile SwitchDisconnector(string? defaultSeries)
    {
        return new ExcelImportProfile(
            ProductTypeCode: "SWITCH_DISCONNECTOR",
            DefaultCabinetKind: null,
            DefaultProductSeries: defaultSeries,
            NameColumn: "Наименование",
            ArticleColumn: "Артикул",
            CodeColumn: "Код",
            ManufacturerColumn: defaultSeries switch
            {
                "ВР32" => "Производитель ВР32 (ВР32)",
                "РЕ19" => "Производитель РЕ19 (РЕ19)",
                _ => "Производитель"
            },
            CharacteristicColumnMap: defaultSeries switch
            {
                "ВР32" => new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["Кол-во полюсов (ВР32)"] = "POLES",
                    ["Номинальный ток (ВР32)"] = "RATED_CURRENT",
                    ["Исполнение (рев/нерев) (ВР32)"] = "REVERSIBLE",
                    ["Способ установки"] = "MOUNTING_METHOD"
                },
                "РЕ19" => new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["Кол-во полюсов (РЕ19)"] = "POLES",
                    ["Номинальный ток (РЕ19)"] = "RATED_CURRENT",
                    ["Исполнение (рев/нерев) (РЕ19)"] = "REVERSIBLE",
                    ["Способ установки"] = "MOUNTING_METHOD"
                },
                _ => new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["Количество полюсов"] = "POLES",
                    ["Номинальный ток"] = "RATED_CURRENT",
                    ["Исполнение (реверс./нереверс.)"] = "REVERSIBLE",
                    ["Способ установки"] = "MOUNTING_METHOD"
                }
            });
    }

    private static ExcelImportProfile LoadSwitch()
    {
        return new ExcelImportProfile(
            ProductTypeCode: "LOAD_SWITCH",
            DefaultCabinetKind: null,
            DefaultProductSeries: null,
            NameColumn: "Наименование",
            ArticleColumn: "Артикул",
            CodeColumn: "Код",
            ManufacturerColumn: "Производитель ВН (Выключатель нагрузки)",
            CharacteristicColumnMap: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Номинальный ток (Выключатель нагрузки)"] = "RATED_CURRENT",
                ["Кол-во полюсов (Выключатель нагрузки)"] = "POLES",
                ["Кол-во модулей (Выключатель нагрузки)"] = "MODULE_COUNT",
                ["Способ установки"] = "MOUNTING_METHOD"
            });
    }
}