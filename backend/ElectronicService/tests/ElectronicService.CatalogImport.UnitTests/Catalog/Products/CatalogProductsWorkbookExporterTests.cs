using ClosedXML.Excel;
using ElectronicService.Infrastructure.Postgres.Catalog.Products;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.CatalogImport.UnitTests.Catalog.Products;

public sealed class CatalogProductsWorkbookExporterTests
{
    [Fact]
    public async Task ExportAsyncCreatesValidWorkbookForEmptySelection()
    {
        var options = new DbContextOptionsBuilder<ElectronicDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=unused;Username=unused;Password=unused")
            .Options;

        await using var dbContext = new ElectronicDbContext(options);
        var exporter = new CatalogProductsWorkbookExporter(dbContext);

        var content = await exporter.ExportAsync(
            [],
            new DateTime(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc),
            TestContext.Current.CancellationToken);

        using var stream = new MemoryStream(content);
        using var workbook = new XLWorkbook(stream);

        var summary = workbook.Worksheet("Сводка");
        var products = workbook.Worksheet("Товары");
        var metadata = workbook.Worksheet("_Справочник");

        Assert.Equal("Товары", workbook.Worksheet(1).Name);
        Assert.Equal("Сводка", workbook.Worksheet(2).Name);
        Assert.Equal("Выгрузка каталога товаров", summary.Cell("A1").GetString());
        Assert.Equal(0d, summary.Cell("B4").GetDouble());
        Assert.Equal("Артикул", products.Cell("B1").GetString());
        Assert.Equal("Производитель", products.Cell("D1").GetString());
        Assert.Equal("0.####", products.Column(9).Style.NumberFormat.Format);
        Assert.Equal("ElectronicCRM.ProductCatalog", metadata.Cell("B1").GetString());
    }
}
