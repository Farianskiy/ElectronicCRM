using CSharpFunctionalExtensions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using ElectronicService.Core.Catalog.PriceLists.Import;
using ElectronicService.Domain.Common;
using ElectronicService.Infrastructure.Postgres.Catalog.PriceLists;

namespace ElectronicService.Infrastructure.UnitTests.Catalog.PriceLists;

public sealed class CatalogPriceListWorkbookReaderTests
{
    [Fact]
    public async Task ReadAsyncReadsArticleAndPriceFromArbitraryColumnsWithoutDate()
    {
        var workbook = CreateWorkbook(
            "Сентябрь",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["C3"] = "Цена",
                ["F3"] = "Артикул",
                ["C4"] = "1 234,50",
                ["F4"] = " ABC-123 "
            });

        var rows = new List<CatalogPriceListSourceRow>();
        var reader = new CatalogPriceListWorkbookReader();

        var result = await reader.ReadAsync(
            "prices.xlsx",
            workbook,
            (row, _) =>
            {
                rows.Add(row);
                return Task.FromResult(
                    UnitResult.Success<DomainError>());
            },
            (_, _) => Task.FromResult(
                UnitResult.Success<DomainError>()),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("Сентябрь", result.Value.WorksheetName);
        Assert.Null(result.Value.EffectiveDate);
        Assert.Equal(3, result.Value.HeaderRowNumber);
        Assert.Equal(1, result.Value.RowsCount);

        var row = Assert.Single(rows);
        Assert.Equal(4, row.RowNumber);
        Assert.Equal("ABC-123", row.Article);
        Assert.Equal(1234.50m, row.BasePriceAmount);
        Assert.Equal(string.Empty, row.Name);
        Assert.Null(row.MrcPriceAmount);
        Assert.Null(row.Unit);
    }

    [Fact]
    public async Task ReadAsyncPrefersPriceWorksheetWhenItExists()
    {
        var workbook = CreateWorkbook(
            ("Справка", new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["A1"] = "Текст"
            }),
            ("Прайс", new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["A1"] = "Артикул",
                ["B1"] = "Цена",
                ["A2"] = "A-1",
                ["B2"] = "10"
            }));

        var rows = new List<CatalogPriceListSourceRow>();
        var reader = new CatalogPriceListWorkbookReader();

        var result = await reader.ReadAsync(
            "prices.xlsx",
            workbook,
            (row, _) =>
            {
                rows.Add(row);
                return Task.FromResult(
                    UnitResult.Success<DomainError>());
            },
            (_, _) => Task.FromResult(
                UnitResult.Success<DomainError>()),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("Прайс", result.Value.WorksheetName);
        Assert.Equal("A-1", Assert.Single(rows).Article);
    }

    [Fact]
    public async Task ReadAsyncFindsDataOnSecondWorksheetWithArbitraryName()
    {
        var workbook = CreateWorkbook(
            ("Обложка", new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["A1"] = "Прайс-лист"
            }),
            ("Данные", new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["D2"] = "Цена",
                ["B2"] = "Артикул",
                ["D3"] = "25.50",
                ["B3"] = "B-2"
            }));

        var rows = new List<CatalogPriceListSourceRow>();
        var reader = new CatalogPriceListWorkbookReader();

        var result = await reader.ReadAsync(
            "prices.xlsx",
            workbook,
            (row, _) =>
            {
                rows.Add(row);
                return Task.FromResult(
                    UnitResult.Success<DomainError>());
            },
            (_, _) => Task.FromResult(
                UnitResult.Success<DomainError>()),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("Данные", result.Value.WorksheetName);
        Assert.Equal("B-2", Assert.Single(rows).Article);
    }

    [Fact]
    public async Task ReadAsyncReturnsHeaderErrorWhenPriceColumnIsMissing()
    {
        var workbook = CreateWorkbook(
            "Данные",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["A1"] = "Артикул",
                ["A2"] = "A-1"
            });

        var reader = new CatalogPriceListWorkbookReader();

        var result = await reader.ReadAsync(
            "prices.xlsx",
            workbook,
            (_, _) => Task.FromResult(
                UnitResult.Success<DomainError>()),
            (_, _) => Task.FromResult(
                UnitResult.Success<DomainError>()),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(
            "catalog.price_list.workbook.header_not_found",
            result.Error.Code);
    }

    private static byte[] CreateWorkbook(
        string worksheetName,
        Dictionary<string, string> cells)
    {
        return CreateWorkbook((worksheetName, cells));
    }

    private static byte[] CreateWorkbook(
        params (string Name, Dictionary<string, string> Cells)[] worksheets)
    {
        using var stream = new MemoryStream();

        using (var document = SpreadsheetDocument.Create(
                   stream,
                   SpreadsheetDocumentType.Workbook,
                   autoSave: true))
        {
            var workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();
            var sheets = workbookPart.Workbook.AppendChild(new Sheets());

            for (var index = 0; index < worksheets.Length; index++)
            {
                var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                var sheetData = new SheetData();
                worksheetPart.Worksheet = new Worksheet();
                worksheetPart.Worksheet.AppendChild(sheetData);

                foreach (var rowGroup in worksheets[index].Cells
                             .GroupBy(item => GetRowNumber(item.Key))
                             .OrderBy(group => group.Key))
                {
                    var row = new Row
                    {
                        RowIndex = (uint)rowGroup.Key
                    };

                    foreach (var item in rowGroup.OrderBy(
                                 item => item.Key,
                                 StringComparer.Ordinal))
                    {
                        var inlineString = new InlineString();
                        inlineString.AppendChild(
                            new Text(item.Value));

                        row.AppendChild(new Cell
                        {
                            CellReference = item.Key,
                            DataType = CellValues.InlineString,
                            InlineString = inlineString
                        });
                    }

                    sheetData.AppendChild(row);
                }

                sheets.AppendChild(new Sheet
                {
                    Id = workbookPart.GetIdOfPart(worksheetPart),
                    SheetId = (uint)(index + 1),
                    Name = worksheets[index].Name
                });
            }

            workbookPart.Workbook.Save();
        }

        return stream.ToArray();
    }

    private static int GetRowNumber(string cellReference)
    {
        return int.Parse(
            new string(
                cellReference
                    .Where(char.IsDigit)
                    .ToArray()),
            System.Globalization.CultureInfo.InvariantCulture);
    }
}
