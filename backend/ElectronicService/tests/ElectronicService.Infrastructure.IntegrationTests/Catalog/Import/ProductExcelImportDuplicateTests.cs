using ElectronicService.Infrastructure.IntegrationTests.Data;
using ElectronicService.Infrastructure.IntegrationTests.Fixtures;
using ElectronicService.Infrastructure.Postgres.Catalog.Import;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ElectronicService.Infrastructure.IntegrationTests.Catalog.Import;

[Collection(PostgreSqlIntegrationDefinition.Name)]
public sealed class ProductExcelImportDuplicateTests
    : PostgreSqlIntegrationTest
{
    public ProductExcelImportDuplicateTests(
        PostgreSqlFixture fixture)
        : base(fixture)
    {
    }

    // Проверяет preview для товара, который уже импортирован:
    // строка получает Duplicate и не считается новой.
    [Fact]
    public async Task PreviewAsyncReportsDuplicateForExistingProduct()
    {
        // Arrange
        await ExcelImportTestDataFactory.EnsureMetadataAsync(
            DbContext,
            TestContext.Current.CancellationToken);

        var row = ExcelImportTestDataFactory.CreateValidRow();
        var service = CreateService();

        using (var importStream =
               ExcelImportTestDataFactory.CreateWorkbook(row))
        {
            var importResult = await service.ImportAsync(
                importStream,
                ExcelImportTestDataFactory.FileName,
                TestContext.Current.CancellationToken);

            Assert.Equal(1, importResult.ImportedRows);
        }

        using var previewStream =
            ExcelImportTestDataFactory.CreateWorkbook(row);

        // Act
        var result = await service.PreviewAsync(
            previewStream,
            ExcelImportTestDataFactory.FileName,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, result.DuplicateRows);
        Assert.Equal(0, result.CreateRows);

        var previewRow = Assert.Single(result.Rows);

        Assert.Equal("Duplicate", previewRow.Action);
    }

    // Проверяет повторный импорт одного файла:
    // второй запуск пропускает товар и не создаёт копию.
    [Fact]
    public async Task ImportAsyncSkipsProductImportedEarlier()
    {
        // Arrange
        await ExcelImportTestDataFactory.EnsureMetadataAsync(
            DbContext,
            TestContext.Current.CancellationToken);

        var row = ExcelImportTestDataFactory.CreateValidRow();
        var service = CreateService();

        using (var firstStream =
               ExcelImportTestDataFactory.CreateWorkbook(row))
        {
            var firstResult = await service.ImportAsync(
                firstStream,
                ExcelImportTestDataFactory.FileName,
                TestContext.Current.CancellationToken);

            Assert.Equal(1, firstResult.ImportedRows);
        }

        using var secondStream =
            ExcelImportTestDataFactory.CreateWorkbook(row);

        // Act
        var secondResult = await service.ImportAsync(
            secondStream,
            ExcelImportTestDataFactory.FileName,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, secondResult.ImportedRows);
        Assert.Equal(1, secondResult.SkippedRows);

        var productsCount = await DbContext.Products.CountAsync(
            product => EF.Functions.ILike(product.Name.Value, row.Name),
            TestContext.Current.CancellationToken);

        Assert.Equal(1, productsCount);
    }

    // Проверяет одинаковые строки внутри одного Excel:
    // первая создаётся, вторая пропускается до SaveChanges.
    [Fact]
    public async Task ImportAsyncSkipsDuplicateRowsInsideSameWorkbook()
    {
        // Arrange
        await ExcelImportTestDataFactory.EnsureMetadataAsync(
            DbContext,
            TestContext.Current.CancellationToken);

        var row = ExcelImportTestDataFactory.CreateValidRow();

        using var stream =
            ExcelImportTestDataFactory.CreateWorkbook(
                row,
                row);

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

        var productsCount = await DbContext.Products.CountAsync(
            product => EF.Functions.ILike(product.Name.Value, row.Name),
            TestContext.Current.CancellationToken);

        Assert.Equal(1, productsCount);
    }

    // Проверяет одинаковые строки в preview:
    // первая строка получает Create, вторая — Duplicate.
    [Fact]
    public async Task PreviewAsyncDetectsDuplicateRowsInsideSameWorkbook()
    {
        // Arrange
        await ExcelImportTestDataFactory.EnsureMetadataAsync(
            DbContext,
            TestContext.Current.CancellationToken);

        var row = ExcelImportTestDataFactory.CreateValidRow();

        using var stream =
            ExcelImportTestDataFactory.CreateWorkbook(
                row,
                row);

        var service = CreateService();

        // Act
        var result = await service.PreviewAsync(
            stream,
            ExcelImportTestDataFactory.FileName,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.TotalRows);
        Assert.Equal(1, result.CreateRows);
        Assert.Equal(1, result.DuplicateRows);

        Assert.Collection(
            result.Rows,
            first => Assert.Equal("Create", first.Action),
            second => Assert.Equal("Duplicate", second.Action));
    }

    // Проверяет, что нормализованное имя ИЕК переиспользует IEK
    // и импорт не создаёт отдельного производителя с кириллическим именем.
    [Fact]
    public async Task ImportAsyncReusesNormalizedManufacturer()
    {
        // Arrange
        var metadata =
            await ExcelImportTestDataFactory.EnsureMetadataAsync(
                DbContext,
                TestContext.Current.CancellationToken);

        var manufacturersBefore =
            await DbContext.Manufacturers.CountAsync(
                TestContext.Current.CancellationToken);

        var row = ExcelImportTestDataFactory.CreateValidRow(
            manufacturer: "ИЕК");

        using var stream =
            ExcelImportTestDataFactory.CreateWorkbook(row);

        var service = CreateService();

        // Act
        var result = await service.ImportAsync(
            stream,
            ExcelImportTestDataFactory.FileName,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, result.ImportedRows);

        var manufacturersAfter =
            await DbContext.Manufacturers.CountAsync(
                TestContext.Current.CancellationToken);

        Assert.Equal(manufacturersBefore, manufacturersAfter);

        var product = await DbContext.Products.SingleAsync(
            item => EF.Functions.ILike(item.Name.Value, row.Name),
            TestContext.Current.CancellationToken);

        Assert.Equal(metadata.Manufacturer.Id, product.ManufacturerId);
    }

    private ProductExcelImportService CreateService()
    {
        return new ProductExcelImportService(
            DbContext,
            NullLogger<ProductExcelImportService>.Instance);
    }
}
