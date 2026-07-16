using System.Globalization;
using ClosedXML.Excel;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.Manufacturers;
using ElectronicService.Domain.Catalog.ProductTypes;
using ElectronicService.Infrastructure.Postgres.Data;
using ElectronicService.TestCommon;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.IntegrationTests.Data;

/// <summary>
/// Подготавливает каталог и создаёт небольшие Excel-файлы
/// для integration-тестов ProductExcelImportService.
/// </summary>
internal static class ExcelImportTestDataFactory
{
    public const string FileName =
        "Модульные автоматы integration.xlsx";

    private const string ProductTypeCode =
        "MODULAR_CIRCUIT_BREAKER";

    private static readonly ExcelCharacteristicSeed[] CharacteristicSeeds =
    [
        new(
            "RATED_CURRENT",
            "Номинальный ток",
            CharacteristicDataType.Number,
            "А",
            IsRequired: true),

        new(
            "POLES",
            "Количество полюсов",
            CharacteristicDataType.Number,
            null,
            IsRequired: true),

        new(
            "CURVE",
            "Характеристика срабатывания",
            CharacteristicDataType.Text,
            null,
            IsRequired: true),

        new(
            "BREAKING_CAPACITY",
            "ПКС",
            CharacteristicDataType.Number,
            "кА",
            IsRequired: true),

        new(
            "HAS_THERMAL_RELEASE",
            "Наличие теплового расцепителя",
            CharacteristicDataType.Boolean,
            null,
            IsRequired: false),

        new(
            "PRODUCT_SERIES",
            "Серия товара",
            CharacteristicDataType.Text,
            null,
            IsRequired: false)
    ];

    public static async Task<ExcelImportMetadata> EnsureMetadataAsync(
        ElectronicDbContext dbContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        var productType = await dbContext.ProductTypes
            .Include(type => type.Characteristics)
            .SingleOrDefaultAsync(
                type => EF.Functions.ILike(
                    type.Code,
                    ProductTypeCode),
                cancellationToken);

        if (productType is null)
        {
            productType = TestDataFactory.CreateProductType(
                ProductTypeCode,
                "Модульный автомат");

            dbContext.ProductTypes.Add(productType);
        }

        var characteristicCodes = CharacteristicSeeds
            .Select(seed => seed.Code)
            .ToList();

        var definitions = await dbContext.CharacteristicDefinitions
            .Where(definition =>
                characteristicCodes.Contains(definition.Code))
            .ToDictionaryAsync(
                definition => definition.Code,
                StringComparer.Ordinal,
                cancellationToken);

        foreach (var seed in CharacteristicSeeds)
        {
            if (definitions.ContainsKey(seed.Code))
            {
                continue;
            }

            var definition =
                TestDataFactory.CreateCharacteristicDefinition(
                    seed.Code,
                    seed.Name,
                    seed.DataType,
                    seed.Unit);

            definitions.Add(seed.Code, definition);
            dbContext.CharacteristicDefinitions.Add(definition);
        }

        foreach (var seed in CharacteristicSeeds)
        {
            var definition = definitions[seed.Code];

            if (productType.AllowsCharacteristic(definition.Id))
            {
                continue;
            }

            TestDataFactory.AddCharacteristic(
                productType,
                definition,
                isRequired: seed.IsRequired,
                isFilterable: true,
                isUsedForReplacement: false,
                replacementMatchMode: ReplacementMatchMode.None,
                replacementWeight: 0);
        }

        var manufacturer = await dbContext.Manufacturers
            .SingleOrDefaultAsync(
                item => EF.Functions.ILike(
                    item.NormalizedName,
                    "IEK"),
                cancellationToken);

        if (manufacturer is null)
        {
            manufacturer = CreateManufacturer("IEK");
            dbContext.Manufacturers.Add(manufacturer);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new ExcelImportMetadata(
            productType,
            manufacturer,
            definitions);
    }

    public static ExcelImportRow CreateValidRow(
        string? name = null,
        string manufacturer = "ИЕК")
    {
        var suffix = Guid.NewGuid().ToString(
            "N",
            CultureInfo.InvariantCulture);

        return new ExcelImportRow(
            Name: name ?? $"Тестовый автомат {suffix}",
            Manufacturer: manufacturer,
            Poles: "2",
            RatedCurrent: "16",
            Curve: "C",
            BreakingCapacity: "6000",
            HasThermalRelease: "да",
            ProductSeries: "Proxima");
    }

    public static MemoryStream CreateWorkbook(
        params ExcelImportRow[] rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        using var workbook = new XLWorkbook();

        var worksheet = workbook.Worksheets.Add("Products");

        worksheet.Cell(1, 1).Value = "Наименование";
        worksheet.Cell(1, 2).Value = "Производитель автоматы";
        worksheet.Cell(1, 3).Value = "Количество полюсов";
        worksheet.Cell(1, 4).Value = "Номинальный ток";
        worksheet.Cell(1, 5).Value = "Характеристика срабатывания";
        worksheet.Cell(1, 6).Value = "ПКС";
        worksheet.Cell(1, 7).Value = "Наличие теплового рсцеп";
        worksheet.Cell(1, 8).Value = "Производитель Серия";

        for (var index = 0; index < rows.Length; index++)
        {
            var rowNumber = index + 2;
            var row = rows[index];

            worksheet.Cell(rowNumber, 1).Value = row.Name;
            worksheet.Cell(rowNumber, 2).Value = row.Manufacturer;
            worksheet.Cell(rowNumber, 3).Value = row.Poles;
            worksheet.Cell(rowNumber, 4).Value = row.RatedCurrent;
            worksheet.Cell(rowNumber, 5).Value = row.Curve;
            worksheet.Cell(rowNumber, 6).Value = row.BreakingCapacity;
            worksheet.Cell(rowNumber, 7).Value = row.HasThermalRelease;
            worksheet.Cell(rowNumber, 8).Value = row.ProductSeries;
        }

        var stream = new MemoryStream();

        workbook.SaveAs(stream);
        stream.Position = 0;

        return stream;
    }

    private static Manufacturer CreateManufacturer(
        string name)
    {
        var result = Manufacturer.Create(name);

        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Не удалось создать тестового производителя: " +
                $"{result.Error.Code}: {result.Error.Message}");
        }

        return result.Value;
    }

    private sealed record ExcelCharacteristicSeed(
        string Code,
        string Name,
        CharacteristicDataType DataType,
        string? Unit,
        bool IsRequired);
}

internal sealed record ExcelImportRow(
    string Name,
    string Manufacturer,
    string Poles,
    string RatedCurrent,
    string Curve,
    string BreakingCapacity,
    string HasThermalRelease,
    string ProductSeries);

internal sealed record ExcelImportMetadata(
    ProductType ProductType,
    Manufacturer Manufacturer,
    IReadOnlyDictionary<string, CharacteristicDefinition> Definitions);
