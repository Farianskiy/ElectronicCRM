using ElectronicService.Infrastructure.IntegrationTests.Data;
using System.Globalization;
using ElectronicService.Infrastructure.IntegrationTests.Fixtures;
using ElectronicService.Infrastructure.Postgres.Catalog.Import;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ElectronicService.Infrastructure.IntegrationTests.Catalog.Import;

[Collection(PostgreSqlIntegrationDefinition.Name)]
public sealed class ProductExcelImportPreviewTests
    : PostgreSqlIntegrationTest
{
    public ProductExcelImportPreviewTests(
        PostgreSqlFixture fixture)
        : base(fixture)
    {
    }

    // Проверяет валидный preview:
    // строка получает Create, ИЕК нормализуется в IEK,
    // а база данных остаётся без новых товаров и производителей.
    [Fact]
    public async Task PreviewAsyncReportsCreateAndDoesNotChangeDatabase()
    {
        // Arrange
        await ExcelImportTestDataFactory.EnsureMetadataAsync(
            DbContext,
            TestContext.Current.CancellationToken);

        var productsBefore = await DbContext.Products.CountAsync(
            TestContext.Current.CancellationToken);

        var manufacturersBefore =
            await DbContext.Manufacturers.CountAsync(
                TestContext.Current.CancellationToken);

        var row = ExcelImportTestDataFactory.CreateValidRow();

        using var stream =
            ExcelImportTestDataFactory.CreateWorkbook(row);

        var service = CreateService();

        // Act
        var result = await service.PreviewAsync(
            stream,
            ExcelImportTestDataFactory.FileName,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, result.TotalRows);
        Assert.Equal(1, result.CreateRows);
        Assert.Equal(0, result.DuplicateRows);
        Assert.Equal(0, result.ErrorRows);
        Assert.Equal(1, result.NormalizedManufacturerRows);
        Assert.Equal(0, result.NewManufacturerRows);

        var previewRow = Assert.Single(result.Rows);

        Assert.Equal("Create", previewRow.Action);
        Assert.Equal("ИЕК", previewRow.RawManufacturerName);
        Assert.Equal("IEK", previewRow.NormalizedManufacturerName);
        Assert.Equal("UseExisting", previewRow.ManufacturerAction);
        Assert.Empty(previewRow.Errors);

        var productsAfter = await DbContext.Products.CountAsync(
            TestContext.Current.CancellationToken);

        var manufacturersAfter =
            await DbContext.Manufacturers.CountAsync(
                TestContext.Current.CancellationToken);

        Assert.Equal(productsBefore, productsAfter);
        Assert.Equal(manufacturersBefore, manufacturersAfter);
    }

    // Проверяет, что неизвестный производитель отмечается CreateNew,
    // но preview не сохраняет его в таблицу manufacturers.
    [Fact]
    public async Task PreviewAsyncReportsNewManufacturerWithoutSavingIt()
    {
        // Arrange
        await ExcelImportTestDataFactory.EnsureMetadataAsync(
            DbContext,
            TestContext.Current.CancellationToken);

        var manufacturerName = string.Concat(
            "Новый производитель ",
            Guid.NewGuid().ToString(
                "N",
                CultureInfo.InvariantCulture));

        var manufacturersBefore =
            await DbContext.Manufacturers.CountAsync(
                TestContext.Current.CancellationToken);

        var row = ExcelImportTestDataFactory.CreateValidRow(
            manufacturer: manufacturerName);

        using var stream =
            ExcelImportTestDataFactory.CreateWorkbook(row);

        var service = CreateService();

        // Act
        var result = await service.PreviewAsync(
            stream,
            ExcelImportTestDataFactory.FileName,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, result.NewManufacturerRows);

        var previewRow = Assert.Single(result.Rows);

        Assert.Equal("CreateNew", previewRow.ManufacturerAction);

        var manufacturersAfter =
            await DbContext.Manufacturers.CountAsync(
                TestContext.Current.CancellationToken);

        Assert.Equal(manufacturersBefore, manufacturersAfter);
    }

    // Проверяет обязательность наименования товара.
    [Fact]
    public async Task PreviewAsyncReturnsErrorForBlankProductName()
    {
        // Arrange
        await ExcelImportTestDataFactory.EnsureMetadataAsync(
            DbContext,
            TestContext.Current.CancellationToken);

        var row = ExcelImportTestDataFactory
            .CreateValidRow() with
        {
            Name = string.Empty
        };

        using var stream =
            ExcelImportTestDataFactory.CreateWorkbook(row);

        var service = CreateService();

        // Act
        var result = await service.PreviewAsync(
            stream,
            ExcelImportTestDataFactory.FileName,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, result.ErrorRows);

        var previewRow = Assert.Single(result.Rows);

        Assert.Equal("Error", previewRow.Action);

        Assert.Contains(
            previewRow.Errors,
            error => error.Contains(
                "Наименование не заполнено",
                StringComparison.Ordinal));
    }

    // Проверяет ошибку разбора числовой характеристики.
    [Fact]
    public async Task PreviewAsyncReturnsErrorForInvalidNumber()
    {
        // Arrange
        await ExcelImportTestDataFactory.EnsureMetadataAsync(
            DbContext,
            TestContext.Current.CancellationToken);

        var row = ExcelImportTestDataFactory
            .CreateValidRow() with
        {
            RatedCurrent = "не число"
        };

        using var stream =
            ExcelImportTestDataFactory.CreateWorkbook(row);

        var service = CreateService();

        // Act
        var result = await service.PreviewAsync(
            stream,
            ExcelImportTestDataFactory.FileName,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, result.ErrorRows);

        var previewRow = Assert.Single(result.Rows);

        Assert.Contains(
            previewRow.Errors,
            error => error.Contains(
                "Cannot parse number",
                StringComparison.Ordinal));
    }

    // Проверяет ошибку отсутствующей обязательной характеристики CURVE.
    [Fact]
    public async Task PreviewAsyncReturnsErrorForMissingRequiredCharacteristic()
    {
        // Arrange
        await ExcelImportTestDataFactory.EnsureMetadataAsync(
            DbContext,
            TestContext.Current.CancellationToken);

        var row = ExcelImportTestDataFactory
            .CreateValidRow() with
        {
            Curve = string.Empty
        };

        using var stream =
            ExcelImportTestDataFactory.CreateWorkbook(row);

        var service = CreateService();

        // Act
        var result = await service.PreviewAsync(
            stream,
            ExcelImportTestDataFactory.FileName,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, result.ErrorRows);

        var previewRow = Assert.Single(result.Rows);

        Assert.Contains(
            previewRow.Errors,
            error => error.Contains(
                "CURVE",
                StringComparison.Ordinal));
    }

    private ProductExcelImportService CreateService()
    {
        return new ProductExcelImportService(
            DbContext,
            NullLogger<ProductExcelImportService>.Instance);
    }
}
