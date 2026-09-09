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

        var requestedQuantity = calculation.Lines.Sum(line => line.Quantity);
        var availableQuantity = calculation.Lines.Sum(line => Math.Min(line.Quantity, line.StockQuantity));
        var shortageQuantity = calculation.Lines.Sum(line => line.ShortageQuantity);
        var shortageLinesCount = calculation.Lines.Count(line => line.ShortageQuantity > 0m);
        var shortageAmount = calculation.Lines.Sum(line => line.ShortageQuantity * line.ProjectPriceAmount);

        worksheet.Cell("A9").Value = "Показатель";
        worksheet.Cell("B9").Value = "Значение";

        ConfigureHeader(worksheet.Range("A9:B9"));

        WriteSummaryRow(worksheet, 10, "Позиций", calculation.Lines.Count);
        WriteSummaryRow(worksheet, 11, "Производителей", calculation.Lines.Select(line => line.ManufacturerId).Distinct().Count());
        WriteSummaryRow(worksheet, 12, "Требуемое количество", requestedQuantity);
        WriteSummaryRow(worksheet, 13, "Доступно для проекта", availableQuantity);
        WriteSummaryRow(worksheet, 14, "Дефицит", shortageQuantity);
        WriteSummaryRow(worksheet, 15, "Позиций с дефицитом", shortageLinesCount);
        WriteSummaryRow(worksheet, 16, "Итоговая сумма", calculation.TotalAmount);
        WriteSummaryRow(worksheet, 17, "Стоимость дефицита", shortageAmount);

        worksheet.Range("B12:B17").Style.NumberFormat.Format = "#,##0.00";
        worksheet.Range("A10:B17").Style.Fill.BackgroundColor = SummaryColor;
        worksheet.Range("A16:B17").Style.Fill.BackgroundColor = TotalColor;
        worksheet.Range("A16:B17").Style.Font.Bold = true;

        const int manufacturerHeaderRow = 20;

        worksheet.Cell(manufacturerHeaderRow, 1).Value = "Производитель";
        worksheet.Cell(manufacturerHeaderRow, 2).Value = "Позиций";
        worksheet.Cell(manufacturerHeaderRow, 3).Value = "Требуется";
        worksheet.Cell(manufacturerHeaderRow, 4).Value = "Доступно";
        worksheet.Cell(manufacturerHeaderRow, 5).Value = "Дефицит";
        worksheet.Cell(manufacturerHeaderRow, 6).Value = "Сумма проекта";
        worksheet.Cell(manufacturerHeaderRow, 7).Value = "Стоимость дефицита";

        ConfigureHeader(worksheet.Range(manufacturerHeaderRow, 1, manufacturerHeaderRow, 7));

        var currentRow = manufacturerHeaderRow + 1;

        foreach (var group in calculation.Lines.GroupBy(line => new { line.ManufacturerId, line.ManufacturerName }).OrderBy(group => group.Key.ManufacturerName, StringComparer.OrdinalIgnoreCase))
        {
            worksheet.Cell(currentRow, 1).Value = ToSafeExcelText(group.Key.ManufacturerName);
            worksheet.Cell(currentRow, 2).Value = group.Count();
            worksheet.Cell(currentRow, 3).Value = group.Sum(line => line.Quantity);
            worksheet.Cell(currentRow, 4).Value = group.Sum(line => Math.Min(line.Quantity, line.StockQuantity));
            worksheet.Cell(currentRow, 5).Value = group.Sum(line => line.ShortageQuantity);
            worksheet.Cell(currentRow, 6).Value = group.Sum(line => line.TotalAmount);
            worksheet.Cell(currentRow, 7).Value = group.Sum(line => line.ShortageQuantity * line.ProjectPriceAmount);

            currentRow++;
        }

        if (currentRow > manufacturerHeaderRow + 1)
        {
            worksheet.Range(manufacturerHeaderRow + 1, 2, currentRow - 1, 7).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Range(manufacturerHeaderRow, 1, currentRow - 1, 7).SetAutoFilter();
        }

        worksheet.Column(1).Width = 34;
        worksheet.Column(2).Width = 18;
        worksheet.Columns(3, 7).Width = 20;
        worksheet.RangeUsed()!.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
    }

    private static void WriteLinesWorksheet(XLWorkbook workbook, CatalogPriceCalculationDetails calculation)
    {
        var worksheet = workbook.Worksheets.Add("Позиции");

        worksheet.ShowGridLines = false;

        var headers = new[] { "№", "Артикул", "Наименование", "Производитель", "Ед.", "Количество", "На складе", "Дефицит", "Прайс 100%", "МРЦ", "Скидка, %", "Проектная цена", "Сумма", "Стоимость дефицита" };

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
            worksheet.Cell(currentRow, 13).Value = line.TotalAmount;
            worksheet.Cell(currentRow, 14).Value = line.ShortageQuantity * line.ProjectPriceAmount;

            if (line.ShortageQuantity > 0m)
            {
                worksheet.Range(currentRow, 8, currentRow, 14).Style.Fill.BackgroundColor = ShortageColor;
            }

            currentRow++;
        }

        if (currentRow > 2)
        {
            worksheet.Range(2, 6, currentRow - 1, 10).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Range(2, 11, currentRow - 1, 11).Style.NumberFormat.Format = "0.00%";
            worksheet.Range(2, 12, currentRow - 1, 14).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Range(1, 1, currentRow - 1, headers.Length).SetAutoFilter();
        }

        worksheet.SheetView.FreezeRows(1);
        worksheet.Column(1).Width = 8;
        worksheet.Column(2).Width = 22;
        worksheet.Column(3).Width = 48;
        worksheet.Column(4).Width = 24;
        worksheet.Column(5).Width = 10;
        worksheet.Columns(6, 14).Width = 18;

        var usedRange = worksheet.RangeUsed();

        if (usedRange is not null)
        {
            usedRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
            usedRange.Style.Alignment.WrapText = true;
        }
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
}