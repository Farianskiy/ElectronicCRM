using ElectronicService.Infrastructure.IntegrationTests.Data;
using ElectronicService.Infrastructure.IntegrationTests.Fixtures;
using ElectronicService.Infrastructure.Postgres.Catalog.Import;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ElectronicService.Infrastructure.IntegrationTests.Catalog.Import;

[Collection(PostgreSqlIntegrationDefinition.Name)]
public sealed class ProductExcelImportPersistenceTests
    : PostgreSqlIntegrationTest
{
    public ProductExcelImportPersistenceTests(
        PostgreSqlFixture fixture)
        : base(fixture)
    {
    }

    // Проверяет фактическое сохранение:
    // товар создаётся с нулевой ценой и остатком,
    // используется существующий IEK и сохраняются все характеристики.
    [Fact]
    public async Task ImportAsyncPersistsProductManufacturerAndCharacteristics()
    {
        // Arrange
        var metadata =
            await ExcelImportTestDataFactory.EnsureMetadataAsync(
                DbContext,
                TestContext.Current.CancellationToken);

        var row = ExcelImportTestDataFactory.CreateValidRow();

        using var stream =
            ExcelImportTestDataFactory.CreateWorkbook(row);

        var service = CreateService();

        // Act
        var result = await service.ImportAsync(
            stream,
            ExcelImportTestDataFactory.FileName,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, result.TotalRows);
        Assert.Equal(1, result.ImportedRows);
        Assert.Equal(0, result.SkippedRows);
        Assert.Empty(result.Errors);

        DbContext.ChangeTracker.Clear();

        var product = await DbContext.Products
            .Include(item => item.Characteristics)
            .SingleAsync(
                item => EF.Functions.ILike(item.Name.Value, row.Name),
                TestContext.Current.CancellationToken);

        Assert.Equal(metadata.ProductType.Id, product.ProductTypeId);
        Assert.Equal(metadata.Manufacturer.Id, product.ManufacturerId);
        Assert.Equal(0m, product.Price.Amount);
        Assert.Equal(0m, product.StockQuantity.Value);
        Assert.Equal(6, product.Characteristics.Count);

        AssertNumberCharacteristic(
            product,
            metadata,
            "POLES",
            2m);

        AssertNumberCharacteristic(
            product,
            metadata,
            "RATED_CURRENT",
            16m);

        AssertNumberCharacteristic(
            product,
            metadata,
            "BREAKING_CAPACITY",
            6m);

        AssertTextCharacteristic(
            product,
            metadata,
            "CURVE",
            "C");

        AssertTextCharacteristic(
            product,
            metadata,
            "PRODUCT_SERIES",
            "Proxima");

        AssertBooleanCharacteristic(
            product,
            metadata,
            "HAS_THERMAL_RELEASE",
            expected: true);
    }

    // Проверяет частичный импорт:
    // валидная строка сохраняется, ошибочная пропускается,
    // а ошибка возвращается в результате.
    [Fact]
    public async Task ImportAsyncSavesValidRowsAndSkipsInvalidRows()
    {
        // Arrange
        await ExcelImportTestDataFactory.EnsureMetadataAsync(
            DbContext,
            TestContext.Current.CancellationToken);

        var validRow = ExcelImportTestDataFactory.CreateValidRow();

        var invalidRow = ExcelImportTestDataFactory
            .CreateValidRow() with
        {
            RatedCurrent = "ошибка"
        };

        using var stream =
            ExcelImportTestDataFactory.CreateWorkbook(
                validRow,
                invalidRow);

        var service = CreateService();

        // Act
        var result = await service.ImportAsync(
            stream,
            ExcelImportTestDataFactory.FileName,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.TotalRows);
        Assert.Equal(1, result.ImportedRows);
        Assert.Equal(1, result.SkippedRows);
        Assert.NotEmpty(result.Errors);

        var validExists = await DbContext.Products.AnyAsync(
            product => EF.Functions.ILike(product.Name.Value, validRow.Name),
            TestContext.Current.CancellationToken);

        var invalidExists = await DbContext.Products.AnyAsync(
            product => EF.Functions.ILike(product.Name.Value, invalidRow.Name),
            TestContext.Current.CancellationToken);

        Assert.True(validExists);
        Assert.False(invalidExists);
    }

    private ProductExcelImportService CreateService()
    {
        return new ProductExcelImportService(
            DbContext,
            NullLogger<ProductExcelImportService>.Instance);
    }

    private static void AssertNumberCharacteristic(
        ElectronicService.Domain.Catalog.Products.Product product,
        ExcelImportMetadata metadata,
        string code,
        decimal expected)
    {
        var definition = metadata.Definitions[code];

        var characteristic = Assert.Single(
            product.Characteristics,
            item =>
                item.CharacteristicDefinitionId == definition.Id);

        Assert.NotNull(characteristic.Value.NumberValue);

        Assert.Equal(
            expected,
            characteristic.Value.NumberValue.Value);
    }

    private static void AssertTextCharacteristic(
        ElectronicService.Domain.Catalog.Products.Product product,
        ExcelImportMetadata metadata,
        string code,
        string expected)
    {
        var definition = metadata.Definitions[code];

        var characteristic = Assert.Single(
            product.Characteristics,
            item =>
                item.CharacteristicDefinitionId == definition.Id);

        Assert.Equal(
            expected,
            characteristic.Value.TextValue);
    }

    private static void AssertBooleanCharacteristic(
        ElectronicService.Domain.Catalog.Products.Product product,
        ExcelImportMetadata metadata,
        string code,
        bool expected)
    {
        var definition = metadata.Definitions[code];

        var characteristic = Assert.Single(
            product.Characteristics,
            item =>
                item.CharacteristicDefinitionId == definition.Id);

        Assert.NotNull(characteristic.Value.BooleanValue);

        Assert.Equal(
            expected,
            characteristic.Value.BooleanValue.Value);
    }
}
