using ClosedXML.Excel;
using ElectronicService.Infrastructure.Postgres.Catalog.Components;

namespace ElectronicService.Infrastructure.UnitTests.Catalog.Components;

public sealed class CatalogComponentCompatibilityWorkbookServiceTests
{
    [Fact]
    public void CreateTemplateCreatesRequiredColumnsAndInstructions()
    {
        var service = new CatalogComponentCompatibilityWorkbookService(null!);

        var content = service.CreateTemplate();

        Assert.NotEmpty(content);

        using var stream = new MemoryStream(content);
        using var workbook = new XLWorkbook(stream);
        var rules = workbook.Worksheet("Правила совместимости");

        Assert.Equal("Артикул комплектующего", rules.Cell(1, 1).GetString());
        Assert.Equal("Тип основного товара", rules.Cell(1, 2).GetString());
        Assert.Equal("Потребность", rules.Cell(1, 3).GetString());
        Assert.True(workbook.TryGetWorksheet("Инструкция", out _));
    }

    [Fact]
    public async Task PreviewRejectsWorkbookWithoutRequiredColumns()
    {
        var service = new CatalogComponentCompatibilityWorkbookService(null!);
        using var workbook = new XLWorkbook();
        workbook.Worksheets.Add("Лист1").Cell(1, 1).Value = "Артикул";
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var result = await service.PreviewAsync(stream, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Contains("Тип основного товара", result.Error.Message, StringComparison.Ordinal);
        Assert.Contains("Потребность", result.Error.Message, StringComparison.Ordinal);
    }
}
