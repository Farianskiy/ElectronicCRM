using ClosedXML.Excel;
using ElectronicService.Core.Catalog.PriceCalculations.Abstractions;
using ElectronicService.Core.Catalog.PriceCalculations.ExportCatalogPriceCalculation;
using ElectronicService.Domain.Catalog.PriceCalculations;

namespace ElectronicService.Infrastructure.Postgres.Catalog.PriceCalculations;

public sealed class CatalogPriceCalculationWorkbookExporter : ICatalogPriceCalculationWorkbookExporter
{
    private static readonly XLColor HeaderColor = XLColor.FromHtml("#0F766E");
    private static readonly XLColor SummaryColor = XLColor.FromHtml("#E6FFFB");
    private static readonly XLColor ShortageColor = XLColor.FromHtml("#FEE2E2");
    private static readonly XLColor TotalColor = XLColor.FromHtml("#DCFCE7");

    public byte[] Export(CatalogPriceCalculationDetails calculation, DateTime generatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(calculation);

        using var workbook = new XLWorkbook();
        WriteSummaryWorksheet(workbook, calculation, generatedAtUtc);
        WriteLinesWorksheet(workbook, calculation);
        WriteComponentsWorksheet(workbook, calculation);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return stream.ToArray();
    }

    private static void WriteSummaryWorksheet(XLWorkbook workbook, CatalogPriceCalculationDetails calculation, DateTime generatedAtUtc)
    {
        var worksheet = workbook.Worksheets.Add("Сводка");

        worksheet.ShowGridLines = false;
        worksheet.Cell("A1").Value = "Расчёт проекта";
        worksheet.Range("A1:B1").Merge();

        ConfigureTitle(worksheet.Range("A1:B1"));

        worksheet.Cell("A3").Value = "Название";
        worksheet.Cell("B3").Value = ToSafeExcelText(calculation.Title);
        worksheet.Cell("A4").Value = "Статус";
        worksheet.Cell("B4").Value = GetStatusLabel(calculation.Status);
        worksheet.Cell("A5").Value = "Создан";
        worksheet.Cell("B5").Value = calculation.CreatedAtUtc;
        worksheet.Cell("A6").Value = "Выгружен";
        worksheet.Cell("B6").Value = generatedAtUtc;
        worksheet.Cell("A7").Value = "Валюта";
        worksheet.Cell("B7").Value = calculation.Currency;

        worksheet.Range("B5:B6").Style.DateFormat.Format = "dd.MM.yyyy HH:mm";
        worksheet.Range("A3:A7").Style.Font.Bold = true;

        var manufacturerItems = CreateManufacturerExportItems(calculation);
        var requestedQuantity = calculation.Lines.Sum(line => line.Quantity);
        var availableQuantity = calculation.Lines.Sum(line => Math.Min(line.Quantity, line.StockQuantity));
        var shortageQuantity = calculation.Lines.Sum(line => line.ShortageQuantity);
        var shortageLinesCount = calculation.Lines.Count(line => line.ShortageQuantity > 0m);
        var shortageAmount = calculation.Lines.Sum(line => line.ShortageQuantity * line.ProjectPriceAmount);
        var componentsCount = calculation.Lines.Sum(line => line.Components.Count);
        var productsAmount = calculation.Lines.Sum(line => line.ProductTotalAmount);
        var componentsAmount = calculation.Lines.Sum(line => line.ComponentsTotalAmount);

        worksheet.Cell("A9").Value = "Показатель";
        worksheet.Cell("B9").Value = "Значение";

        ConfigureHeader(worksheet.Range("A9:B9"));

        WriteSummaryRow(worksheet, 10, "Основных позиций", calculation.Lines.Count);
        WriteSummaryRow(worksheet, 11, "Комплектующих", componentsCount);
        WriteSummaryRow(worksheet, 12, "Производителей", manufacturerItems.Select(item => item.ManufacturerId).Distinct().Count());
        WriteSummaryRow(worksheet, 13, "Требуемое количество основных товаров", requestedQuantity);
        WriteSummaryRow(worksheet, 14, "Доступно основных товаров", availableQuantity);
        WriteSummaryRow(worksheet, 15, "Дефицит основных товаров", shortageQuantity);
        WriteSummaryRow(worksheet, 16, "Основных позиций с дефицитом", shortageLinesCount);
        WriteSummaryRow(worksheet, 17, "Стоимость основных товаров", productsAmount);
        WriteSummaryRow(worksheet, 18, "Стоимость комплектующих", componentsAmount);
        WriteSummaryRow(worksheet, 19, "Итоговая сумма", calculation.TotalAmount);
        WriteSummaryRow(worksheet, 20, "Стоимость дефицита", shortageAmount);

        worksheet.Range("B13:B20").Style.NumberFormat.Format = "#,##0.00";
        worksheet.Range("A10:B20").Style.Fill.BackgroundColor = SummaryColor;
        worksheet.Range("A19:B20").Style.Fill.BackgroundColor = TotalColor;
        worksheet.Range("A19:B20").Style.Font.Bold = true;

        const int manufacturerHeaderRow = 23;

        worksheet.Cell(manufacturerHeaderRow, 1).Value = "Производитель";
        worksheet.Cell(manufacturerHeaderRow, 2).Value = "Основных позиций";
        worksheet.Cell(manufacturerHeaderRow, 3).Value = "Комплектующих";
        worksheet.Cell(manufacturerHeaderRow, 4).Value = "Требуется основных";
        worksheet.Cell(manufacturerHeaderRow, 5).Value = "Доступно основных";
        worksheet.Cell(manufacturerHeaderRow, 6).Value = "Дефицит основных";
        worksheet.Cell(manufacturerHeaderRow, 7).Value = "Основные товары";
        worksheet.Cell(manufacturerHeaderRow, 8).Value = "Комплектующие";
        worksheet.Cell(manufacturerHeaderRow, 9).Value = "Сумма проекта";
        worksheet.Cell(manufacturerHeaderRow, 10).Value = "Стоимость дефицита";

        ConfigureHeader(worksheet.Range(manufacturerHeaderRow, 1, manufacturerHeaderRow, 10));

        var currentRow = manufacturerHeaderRow + 1;

        foreach (var group in manufacturerItems
                     .GroupBy(item => new { item.ManufacturerId, item.ManufacturerName })
                     .OrderBy(group => group.Key.ManufacturerName, StringComparer.OrdinalIgnoreCase))
        {
            worksheet.Cell(currentRow, 1).Value = ToSafeExcelText(group.Key.ManufacturerName);
            worksheet.Cell(currentRow, 2).Value = group.Count(item => !item.IsComponent);
            worksheet.Cell(currentRow, 3).Value = group.Count(item => item.IsComponent);
            worksheet.Cell(currentRow, 4).Value = group.Where(item => !item.IsComponent).Sum(item => item.Quantity);
            worksheet.Cell(currentRow, 5).Value = group.Where(item => !item.IsComponent).Sum(item => item.AvailableQuantity);
            worksheet.Cell(currentRow, 6).Value = group.Where(item => !item.IsComponent).Sum(item => item.ShortageQuantity);
            worksheet.Cell(currentRow, 7).Value = group.Where(item => !item.IsComponent).Sum(item => item.TotalAmount);
            worksheet.Cell(currentRow, 8).Value = group.Where(item => item.IsComponent).Sum(item => item.TotalAmount);
            worksheet.Cell(currentRow, 9).Value = group.Sum(item => item.TotalAmount);
            worksheet.Cell(currentRow, 10).Value = group.Sum(item => item.ShortageAmount);

            currentRow++;
        }

        if (currentRow > manufacturerHeaderRow + 1)
        {
            worksheet.Range(manufacturerHeaderRow + 1, 2, currentRow - 1, 10).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Range(manufacturerHeaderRow, 1, currentRow - 1, 10).SetAutoFilter();
        }

        worksheet.Column(1).Width = 34;
        worksheet.Column(2).Width = 18;
        worksheet.Columns(3, 10).Width = 20;
        worksheet.RangeUsed()!.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
    }

    private static void WriteLinesWorksheet(XLWorkbook workbook, CatalogPriceCalculationDetails calculation)
    {
        var worksheet = workbook.Worksheets.Add("Позиции");

        worksheet.ShowGridLines = false;

        var headers = new[]
        {
            "№",
            "Артикул",
            "Наименование",
            "Производитель",
            "Ед.",
            "Количество",
            "На складе",
            "Дефицит",
            "Прайс 100%",
            "МРЦ",
            "Скидка, %",
            "Проектная цена",
            "Основной товар",
            "Комплектующие",
            "Итого по позиции",
            "Стоимость дефицита"
        };

        for (var index = 0; index < headers.Length; index++)
        {
            worksheet.Cell(1, index + 1).Value = headers[index];
        }

        ConfigureHeader(worksheet.Range(1, 1, 1, headers.Length));

        var currentRow = 2;

        foreach (var line in calculation.Lines.OrderBy(line => line.ManufacturerName, StringComparer.OrdinalIgnoreCase).ThenBy(line => line.Name, StringComparer.OrdinalIgnoreCase))
        {
            worksheet.Cell(currentRow, 1).Value = currentRow - 1;
            worksheet.Cell(currentRow, 2).Value = ToSafeExcelText(line.Article);
            worksheet.Cell(currentRow, 3).Value = ToSafeExcelText(line.Name);
            worksheet.Cell(currentRow, 4).Value = ToSafeExcelText(line.ManufacturerName);
            worksheet.Cell(currentRow, 5).Value = ToSafeExcelText(line.Unit);
            worksheet.Cell(currentRow, 6).Value = line.Quantity;
            worksheet.Cell(currentRow, 7).Value = line.StockQuantity;
            worksheet.Cell(currentRow, 8).Value = line.ShortageQuantity;
            worksheet.Cell(currentRow, 9).Value = line.BasePriceAmount;

            if (line.MrcPriceAmount.HasValue)
            {
                worksheet.Cell(currentRow, 10).Value = line.MrcPriceAmount.Value;
            }

            worksheet.Cell(currentRow, 11).Value = line.DiscountPercent / 100m;
            worksheet.Cell(currentRow, 12).Value = line.ProjectPriceAmount;
            worksheet.Cell(currentRow, 13).Value = line.ProductTotalAmount;
            worksheet.Cell(currentRow, 14).Value = line.ComponentsTotalAmount;
            worksheet.Cell(currentRow, 15).Value = line.TotalAmount;
            worksheet.Cell(currentRow, 16).Value = line.ShortageQuantity * line.ProjectPriceAmount;

            if (line.ShortageQuantity > 0m)
            {
                worksheet.Range(currentRow, 8, currentRow, 16).Style.Fill.BackgroundColor = ShortageColor;
            }

            currentRow++;
        }

        if (currentRow > 2)
        {
            worksheet.Range(2, 6, currentRow - 1, 10).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Range(2, 11, currentRow - 1, 11).Style.NumberFormat.Format = "0.00%";
            worksheet.Range(2, 12, currentRow - 1, 16).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Range(1, 1, currentRow - 1, headers.Length).SetAutoFilter();
        }

        worksheet.SheetView.FreezeRows(1);
        worksheet.Column(1).Width = 8;
        worksheet.Column(2).Width = 22;
        worksheet.Column(3).Width = 48;
        worksheet.Column(4).Width = 24;
        worksheet.Column(5).Width = 10;
        worksheet.Columns(6, 16).Width = 18;

        var usedRange = worksheet.RangeUsed();

        if (usedRange is not null)
        {
            usedRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
            usedRange.Style.Alignment.WrapText = true;
        }
    }

    private static void WriteComponentsWorksheet(
        XLWorkbook workbook,
        CatalogPriceCalculationDetails calculation)
    {
        var worksheet = workbook.Worksheets.Add("Комплектующие");
        worksheet.ShowGridLines = false;

        var headers = new[]
        {
            "№",
            "Артикул основного товара",
            "Основной товар",
            "Количество основного товара",
            "Потребность",
            "Артикул комплектующего",
            "Комплектующее",
            "Производитель",
            "На единицу",
            "Всего",
            "Прайс 100%",
            "Скидка, %",
            "Проектная цена",
            "Сумма"
        };

        for (var index = 0; index < headers.Length; index++)
        {
            worksheet.Cell(1, index + 1).Value = headers[index];
        }

        ConfigureHeader(worksheet.Range(1, 1, 1, headers.Length));

        var currentRow = 2;

        foreach (var line in calculation.Lines
                     .OrderBy(line => line.Name, StringComparer.OrdinalIgnoreCase))
        {
            foreach (var component in line.Components
                         .OrderBy(component => component.NeedName, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(component => component.Name, StringComparer.OrdinalIgnoreCase))
            {
                worksheet.Cell(currentRow, 1).Value = currentRow - 1;
                worksheet.Cell(currentRow, 2).Value = ToSafeExcelText(line.Article);
                worksheet.Cell(currentRow, 3).Value = ToSafeExcelText(line.Name);
                worksheet.Cell(currentRow, 4).Value = line.Quantity;
                worksheet.Cell(currentRow, 5).Value = ToSafeExcelText(component.NeedName);
                worksheet.Cell(currentRow, 6).Value = ToSafeExcelText(component.Article);
                worksheet.Cell(currentRow, 7).Value = ToSafeExcelText(component.Name);
                worksheet.Cell(currentRow, 8).Value = ToSafeExcelText(component.ManufacturerName);
                worksheet.Cell(currentRow, 9).Value = component.QuantityPerUnit;
                worksheet.Cell(currentRow, 10).Value = component.TotalQuantity;
                worksheet.Cell(currentRow, 11).Value = component.BasePriceAmount;
                worksheet.Cell(currentRow, 12).Value = component.DiscountPercent / 100m;
                worksheet.Cell(currentRow, 13).Value = component.ProjectPriceAmount;
                worksheet.Cell(currentRow, 14).Value = component.TotalAmount;

                currentRow++;
            }
        }

        if (currentRow == 2)
        {
            worksheet.Cell(3, 1).Value = "В расчёте нет комплектующих.";
            worksheet.Range(3, 1, 3, headers.Length).Merge();
            worksheet.Cell(3, 1).Style.Font.FontColor = XLColor.Gray;
        }
        else
        {
            worksheet.Range(2, 4, currentRow - 1, 11).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Range(2, 12, currentRow - 1, 12).Style.NumberFormat.Format = "0.00%";
            worksheet.Range(2, 13, currentRow - 1, 14).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Range(1, 1, currentRow - 1, headers.Length).SetAutoFilter();
        }

        worksheet.SheetView.FreezeRows(1);
        worksheet.Column(1).Width = 8;
        worksheet.Column(2).Width = 24;
        worksheet.Column(3).Width = 42;
        worksheet.Column(4).Width = 20;
        worksheet.Column(5).Width = 30;
        worksheet.Column(6).Width = 24;
        worksheet.Column(7).Width = 42;
        worksheet.Column(8).Width = 24;
        worksheet.Columns(9, 14).Width = 18;

        var usedRange = worksheet.RangeUsed();

        if (usedRange is not null)
        {
            usedRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
            usedRange.Style.Alignment.WrapText = true;
        }
    }

    private static List<ManufacturerExportItem> CreateManufacturerExportItems(
        CatalogPriceCalculationDetails calculation)
    {
        var items = new List<ManufacturerExportItem>();

        foreach (var line in calculation.Lines)
        {
            items.Add(new ManufacturerExportItem(
                line.ManufacturerId,
                line.ManufacturerName,
                false,
                line.Quantity,
                Math.Min(line.Quantity, line.StockQuantity),
                line.ShortageQuantity,
                line.ProductTotalAmount,
                line.ShortageQuantity * line.ProjectPriceAmount));

            items.AddRange(line.Components.Select(component => new ManufacturerExportItem(
                component.ManufacturerId,
                component.ManufacturerName,
                true,
                component.TotalQuantity,
                0m,
                0m,
                component.TotalAmount,
                0m)));
        }

        return items;
    }

    private static void WriteSummaryRow(IXLWorksheet worksheet, int row, string label, decimal value)
    {
        worksheet.Cell(row, 1).Value = label;
        worksheet.Cell(row, 2).Value = value;
    }

    private static void ConfigureTitle(IXLRange range)
    {
        range.Style.Font.Bold = true;
        range.Style.Font.FontSize = 16;
        range.Style.Font.FontColor = XLColor.White;
        range.Style.Fill.BackgroundColor = HeaderColor;
    }

    private static void ConfigureHeader(IXLRange range)
    {
        range.Style.Font.Bold = true;
        range.Style.Font.FontColor = XLColor.White;
        range.Style.Fill.BackgroundColor = HeaderColor;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
    }

    private static string GetStatusLabel(CatalogPriceCalculationStatus status)
    {
        return status switch
        {
            CatalogPriceCalculationStatus.Draft => "Черновик",
            CatalogPriceCalculationStatus.Completed => "Завершён",
            CatalogPriceCalculationStatus.Archived => "В архиве",
            _ => status.ToString()
        };
    }

    private static string ToSafeExcelText(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value[0] is '=' or '+' or '-' or '@' ? $"'{value}" : value;
    }

    private sealed record ManufacturerExportItem(
        Guid ManufacturerId,
        string ManufacturerName,
        bool IsComponent,
        decimal Quantity,
        decimal AvailableQuantity,
        decimal ShortageQuantity,
        decimal TotalAmount,
        decimal ShortageAmount);
}
