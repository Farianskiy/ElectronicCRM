using ClosedXML.Excel;
using ElectronicService.Core.Catalog.ImportBatches.Analysis;
using ElectronicService.Core.Catalog.ImportBatches.ExportCatalogImportErrorReport;

namespace ElectronicService.Infrastructure.Postgres.Catalog.ImportBatches.Reports;

public sealed class CatalogImportErrorReportGenerator
    : ICatalogImportErrorReportGenerator
{
    private static readonly XLColor HeaderColor =
        XLColor.FromHtml("#0F766E");

    private static readonly XLColor ErrorColor =
        XLColor.FromHtml("#FEE2E2");

    private static readonly XLColor WarningColor =
        XLColor.FromHtml("#FEF3C7");

    public byte[] Generate(
        CatalogImportErrorReportData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        using var workbook = new XLWorkbook();

        WriteSummaryWorksheet(
            workbook,
            data);

        WriteErrorRowsWorksheet(
            workbook,
            data);

        WriteIssuesWorksheet(
            workbook,
            data);

        using var stream = new MemoryStream();

        workbook.SaveAs(stream);

        return stream.ToArray();
    }

    private static void WriteSummaryWorksheet(
        XLWorkbook workbook,
        CatalogImportErrorReportData data)
    {
        var worksheet =
            workbook.Worksheets.Add("Сводка");

        worksheet.Cell(1, 1).Value =
            "Отчёт об ошибках импорта каталога";

        worksheet.Range(1, 1, 1, 2).Merge();

        var titleRange =
            worksheet.Range(1, 1, 1, 2);

        titleRange.Style.Font.Bold = true;
        titleRange.Style.Font.FontSize = 16;
        titleRange.Style.Font.FontColor =
            XLColor.White;

        titleRange.Style.Fill.BackgroundColor =
            HeaderColor;

        worksheet.Cell(3, 1).Value = "Пакет";
        worksheet.Cell(3, 2).Value =
            data.BatchId.ToString();

        worksheet.Cell(4, 1).Value =
            "Исходный файл";

        worksheet.Cell(4, 2).Value =
            ToSafeExcelText(
                data.OriginalFileName);

        worksheet.Cell(5, 1).Value = "Статус";
        worksheet.Cell(5, 2).Value = data.Status;

        worksheet.Cell(6, 1).Value =
            "Всего строк";

        worksheet.Cell(6, 2).Value =
            data.RowsCount;

        worksheet.Cell(7, 1).Value =
            "Корректных строк";

        worksheet.Cell(7, 2).Value =
            data.ValidRowsCount;

        worksheet.Cell(8, 1).Value =
            "Строк с ошибками";

        worksheet.Cell(8, 2).Value =
            data.ErrorRowsCount;

        worksheet.Cell(9, 1).Value =
            "Пакет создан, UTC";

        worksheet.Cell(9, 2).Value =
            data.CreatedAtUtc;

        worksheet.Cell(10, 1).Value =
            "Отчёт сформирован, UTC";

        worksheet.Cell(10, 2).Value =
            data.GeneratedAtUtc;

        worksheet.Range(9, 2, 10, 2)
            .Style.DateFormat.Format =
                "dd.MM.yyyy HH:mm:ss";

        worksheet.Range(3, 1, 10, 1)
            .Style.Font.Bold = true;

        var summaryRange =
            worksheet.Range(3, 1, 10, 2);

        summaryRange.Style.Border.InsideBorder =
            XLBorderStyleValues.Thin;

        summaryRange.Style.Border.OutsideBorder =
            XLBorderStyleValues.Thin;

        summaryRange.Style.Alignment.Vertical =
            XLAlignmentVerticalValues.Top;

        summaryRange.Style.Alignment.WrapText =
            true;

        worksheet.Column(1).Width = 28;
        worksheet.Column(2).Width = 80;
    }

    private static void WriteErrorRowsWorksheet(
        XLWorkbook workbook,
        CatalogImportErrorReportData data)
    {
        var worksheet =
            workbook.Worksheets.Add(
                "Ошибочные строки");

        var sourceColumns = data.Columns
            .OrderBy(
                column =>
                    column.SourceColumnNumber)
            .ToArray();

        const int rowNumberColumn = 1;
        const int statusColumn = 2;
        const int firstSourceColumn = 3;

        var issuesColumn =
            firstSourceColumn +
            sourceColumns.Length;

        var warningsColumn =
            issuesColumn + 1;

        worksheet.Cell(
            1,
            rowNumberColumn).Value =
                "Строка Excel";

        worksheet.Cell(
            1,
            statusColumn).Value =
                "Статус";

        for (
            var index = 0;
            index < sourceColumns.Length;
            index++)
        {
            worksheet.Cell(
                1,
                firstSourceColumn + index).Value =
                    ToSafeExcelText(
                        sourceColumns[index]
                            .SourceHeader);
        }

        worksheet.Cell(
            1,
            issuesColumn).Value =
                "Ошибки";

        worksheet.Cell(
            1,
            warningsColumn).Value =
                "Предупреждения";

        var currentRow = 2;

        foreach (
            var reportRow in
            data.ErrorRows.OrderBy(
                row => row.RowNumber))
        {
            worksheet.Cell(
                currentRow,
                rowNumberColumn).Value =
                    reportRow.RowNumber;

            worksheet.Cell(
                currentRow,
                statusColumn).Value =
                    "Ошибка";

            for (
                var index = 0;
                index < sourceColumns.Length;
                index++)
            {
                var sourceColumn =
                    sourceColumns[index];

                reportRow.RawData.TryGetValue(
                    sourceColumn.SourceColumnNumber,
                    out var rawValue);

                worksheet.Cell(
                    currentRow,
                    firstSourceColumn + index)
                    .Value =
                        ToSafeExcelText(rawValue);
            }

            worksheet.Cell(
                currentRow,
                issuesColumn).Value =
                    JoinIssues(
                        reportRow.Issues);

            worksheet.Cell(
                currentRow,
                warningsColumn).Value =
                    JoinIssues(
                        reportRow.Warnings);

            worksheet.Cell(
                currentRow,
                issuesColumn)
                .Style.Fill.BackgroundColor =
                    ErrorColor;

            if (reportRow.Warnings.Count > 0)
            {
                worksheet.Cell(
                    currentRow,
                    warningsColumn)
                    .Style.Fill.BackgroundColor =
                        WarningColor;
            }

            currentRow++;
        }

        var lastColumn = warningsColumn;

        var headerRange =
            worksheet.Range(
                1,
                1,
                1,
                lastColumn);

        ConfigureHeader(headerRange);

        worksheet.SheetView.FreezeRows(1);
        worksheet.RangeUsed()?.SetAutoFilter();

        worksheet.Column(
            rowNumberColumn).Width = 14;

        worksheet.Column(
            statusColumn).Width = 14;

        for (
            var column = firstSourceColumn;
            column < issuesColumn;
            column++)
        {
            worksheet.Column(column).Width = 20;
        }

        worksheet.Column(
            issuesColumn).Width = 70;

        worksheet.Column(
            warningsColumn).Width = 55;

        var usedRange =
            worksheet.RangeUsed();

        if (usedRange is not null)
        {
            usedRange.Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Top;

            usedRange.Style.Alignment.WrapText =
                true;
        }
    }

    private static void WriteIssuesWorksheet(
        XLWorkbook workbook,
        CatalogImportErrorReportData data)
    {
        var worksheet =
            workbook.Worksheets.Add(
                "Список проблем");

        worksheet.Cell(1, 1).Value =
            "Строка Excel";

        worksheet.Cell(1, 2).Value =
            "Тип";

        worksheet.Cell(1, 3).Value =
            "Код";

        worksheet.Cell(1, 4).Value =
            "Поле";

        worksheet.Cell(1, 5).Value =
            "Номер колонки";

        worksheet.Cell(1, 6).Value =
            "Заголовок колонки";

        worksheet.Cell(1, 7).Value =
            "Сообщение";

        var headersByNumber =
            data.Columns.ToDictionary(
                column =>
                    column.SourceColumnNumber,
                column =>
                    column.SourceHeader);

        var currentRow = 2;

        foreach (
            var reportRow in
            data.ErrorRows.OrderBy(
                row => row.RowNumber))
        {
            foreach (
                var issue in reportRow.Issues)
            {
                WriteIssueRow(
                    worksheet,
                    currentRow,
                    reportRow.RowNumber,
                    "Ошибка",
                    issue,
                    headersByNumber);

                worksheet.Range(
                    currentRow,
                    1,
                    currentRow,
                    7)
                    .Style.Fill.BackgroundColor =
                        ErrorColor;

                currentRow++;
            }

            foreach (
                var warning in
                reportRow.Warnings)
            {
                WriteIssueRow(
                    worksheet,
                    currentRow,
                    reportRow.RowNumber,
                    "Предупреждение",
                    warning,
                    headersByNumber);

                worksheet.Range(
                    currentRow,
                    1,
                    currentRow,
                    7)
                    .Style.Fill.BackgroundColor =
                        WarningColor;

                currentRow++;
            }
        }

        var headerRange =
            worksheet.Range(1, 1, 1, 7);

        ConfigureHeader(headerRange);

        worksheet.SheetView.FreezeRows(1);
        worksheet.RangeUsed()?.SetAutoFilter();

        worksheet.Column(1).Width = 14;
        worksheet.Column(2).Width = 18;
        worksheet.Column(3).Width = 38;
        worksheet.Column(4).Width = 24;
        worksheet.Column(5).Width = 16;
        worksheet.Column(6).Width = 32;
        worksheet.Column(7).Width = 80;

        var usedRange =
            worksheet.RangeUsed();

        if (usedRange is not null)
        {
            usedRange.Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Top;

            usedRange.Style.Alignment.WrapText =
                true;
        }
    }

    private static void ConfigureHeader(
        IXLRange headerRange)
    {
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontColor =
            XLColor.White;

        headerRange.Style.Fill.BackgroundColor =
            HeaderColor;

        headerRange.Style.Alignment.Horizontal =
            XLAlignmentHorizontalValues.Center;

        headerRange.Style.Alignment.Vertical =
            XLAlignmentVerticalValues.Center;
    }

    private static void WriteIssueRow(
        IXLWorksheet worksheet,
        int targetRow,
        int sourceRowNumber,
        string issueType,
        CatalogImportRowIssue issue,
        Dictionary<int, string> headersByNumber)
    {
        worksheet.Cell(
            targetRow,
            1).Value =
                sourceRowNumber;

        worksheet.Cell(
            targetRow,
            2).Value =
                issueType;

        worksheet.Cell(
            targetRow,
            3).Value =
                ToSafeExcelText(
                    issue.Code);

        worksheet.Cell(
            targetRow,
            4).Value =
                ToSafeExcelText(
                    issue.Field);

        if (issue.SourceColumnNumber.HasValue)
        {
            worksheet.Cell(
                targetRow,
                5).Value =
                    issue.SourceColumnNumber.Value;

            headersByNumber.TryGetValue(
                issue.SourceColumnNumber.Value,
                out var sourceHeader);

            worksheet.Cell(
                targetRow,
                6).Value =
                    ToSafeExcelText(
                        sourceHeader);
        }

        worksheet.Cell(
            targetRow,
            7).Value =
                ToSafeExcelText(
                    issue.Message);
    }

    private static string JoinIssues(
        IReadOnlyCollection<
            CatalogImportRowIssue> issues)
    {
        if (issues.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(
            Environment.NewLine,
            issues.Select(
                issue =>
                    $"[{issue.Code}] " +
                    issue.Message));
    }

    private static string ToSafeExcelText(
        string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var firstCharacter = value[0];

        if (
            firstCharacter is
                '=' or '+' or '-' or '@'
        )
        {
            return $"'{value}";
        }

        return value;
    }
}