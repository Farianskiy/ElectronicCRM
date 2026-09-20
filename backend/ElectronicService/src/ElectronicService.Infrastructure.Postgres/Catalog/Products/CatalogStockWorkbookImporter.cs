using System.Globalization;
using ClosedXML.Excel;
using ElectronicService.Core.Catalog.Products.StockImport;
using ElectronicService.Domain.Catalog.ValueObjects;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Products;

public sealed class CatalogStockWorkbookImporter
    : ICatalogStockWorkbookImporter
{
    private const int MaximumRowsCount = 100_000;
    private const int MaximumReportedIssuesCount = 20;

    private static readonly string[] ArticleHeaders =
        ["АРТИКУЛ", "ARTICLE", "SKU"];

    private static readonly string[] StockHeaders =
        ["ОСТАТОК", "КОЛИЧЕСТВО", "STOCK", "QUANTITY"];

    private readonly ElectronicDbContext _dbContext;

    public CatalogStockWorkbookImporter(
        ElectronicDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CatalogStockImportResult> ImportAsync(
        Guid manufacturerId,
        Stream workbookStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workbookStream);

        if (manufacturerId == Guid.Empty)
        {
            throw new InvalidDataException(
                "Производитель не выбран.");
        }

        if (!string.Equals(
                Path.GetExtension(fileName),
                ".xlsx",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                "Поддерживаются только файлы XLSX.");
        }

        XLWorkbook workbook;

        try
        {
            workbook = new XLWorkbook(workbookStream);
        }
        catch (Exception exception)
            when (exception is not OperationCanceledException)
        {
            throw new InvalidDataException(
                "Не удалось прочитать XLSX-файл.",
                exception);
        }

        using (workbook)
        {
            var worksheet = workbook.Worksheets.FirstOrDefault()
                ?? throw new InvalidDataException(
                    "В файле нет листов.");

            var header = FindHeader(worksheet);
            var lastRowNumber = worksheet.LastRowUsed()?.RowNumber()
                ?? header.RowNumber;

            if (lastRowNumber - header.RowNumber > MaximumRowsCount)
            {
                throw new InvalidDataException(
                    $"Файл содержит больше {MaximumRowsCount:N0} строк.");
            }

            var parsedRows = ParseRows(
                worksheet,
                header,
                lastRowNumber);

            var manufacturerExists = await _dbContext.Manufacturers
                .AsNoTracking()
                .AnyAsync(
                    manufacturer => manufacturer.Id == manufacturerId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (!manufacturerExists)
            {
                throw new InvalidDataException(
                    "Выбранный производитель не найден.");
            }

            var productReferences = await _dbContext.Products
                .AsNoTracking()
                .Where(product =>
                    product.ManufacturerId == manufacturerId)
                .Select(product => new
                {
                    product.Id,
                    Article = product.Article.Value,
                })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var productsByArticle = productReferences
                .GroupBy(
                    product => NormalizeArticle(product.Article),
                    StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(product => product.Id).ToArray(),
                    StringComparer.Ordinal);

            var updates = new Dictionary<Guid, decimal>();
            var issues = parsedRows.Issues.ToList();
            var matchedRowsCount = 0;
            var skippedRowsCount = parsedRows.SkippedRowsCount;

            foreach (var row in parsedRows.Rows)
            {
                if (!productsByArticle.TryGetValue(
                        row.NormalizedArticle,
                        out var productIds))
                {
                    skippedRowsCount++;
                    AddIssue(
                        issues,
                        row.RowNumber,
                        row.Article,
                        "Товар с таким артикулом у выбранного производителя не найден.");
                    continue;
                }

                if (productIds.Length != 1)
                {
                    skippedRowsCount++;
                    AddIssue(
                        issues,
                        row.RowNumber,
                        row.Article,
                        "В каталоге найдено несколько товаров с таким артикулом.");
                    continue;
                }

                matchedRowsCount++;
                updates[productIds[0]] = row.Quantity;
            }

            var productIdsToUpdate = updates.Keys.ToArray();
            var products = await _dbContext.Products
                .Where(product => productIdsToUpdate.Contains(product.Id))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var updatedProductsCount = 0;

            foreach (var product in products)
            {
                var quantity = updates[product.Id];

                if (product.StockQuantity.Value == quantity)
                {
                    continue;
                }

                var stockQuantity = StockQuantity.Create(quantity).Value;
                product.ChangeStockQuantity(stockQuantity);
                updatedProductsCount++;
            }

            await _dbContext.SaveChangesAsync(cancellationToken)
                .ConfigureAwait(false);

            return new CatalogStockImportResult(
                parsedRows.ReadRowsCount,
                matchedRowsCount,
                updatedProductsCount,
                skippedRowsCount,
                issues);
        }
    }

    private static ParsedWorkbookRows ParseRows(
        IXLWorksheet worksheet,
        HeaderLocation header,
        int lastRowNumber)
    {
        var rows = new List<ParsedStockRow>();
        var issues = new List<CatalogStockImportIssue>();
        var seenArticles = new HashSet<string>(StringComparer.Ordinal);
        var readRowsCount = 0;
        var skippedRowsCount = 0;

        for (var rowNumber = header.RowNumber + 1;
             rowNumber <= lastRowNumber;
             rowNumber++)
        {
            var row = worksheet.Row(rowNumber);
            var article = row.Cell(header.ArticleColumn)
                .GetFormattedString()
                .Trim();
            var stockCell = row.Cell(header.StockColumn);
            var stockText = stockCell.GetFormattedString().Trim();

            if (article.Length == 0 && stockText.Length == 0)
            {
                continue;
            }

            readRowsCount++;

            if (article.Length == 0)
            {
                skippedRowsCount++;
                AddIssue(issues, rowNumber, article, "Не указан артикул.");
                continue;
            }

            if (!TryReadQuantity(stockCell, stockText, out var quantity)
                || quantity < 0)
            {
                skippedRowsCount++;
                AddIssue(
                    issues,
                    rowNumber,
                    article,
                    "Остаток должен быть числом не меньше нуля.");
                continue;
            }

            var normalizedArticle = NormalizeArticle(article);

            if (!seenArticles.Add(normalizedArticle))
            {
                skippedRowsCount++;
                AddIssue(
                    issues,
                    rowNumber,
                    article,
                    "Повторяющийся артикул в файле пропущен.");
                continue;
            }

            rows.Add(new ParsedStockRow(
                rowNumber,
                article,
                normalizedArticle,
                quantity));
        }

        return new ParsedWorkbookRows(
            rows,
            issues,
            readRowsCount,
            skippedRowsCount);
    }

    private static HeaderLocation FindHeader(IXLWorksheet worksheet)
    {
        var lastHeaderRow = Math.Min(
            worksheet.LastRowUsed()?.RowNumber() ?? 1,
            20);

        for (var rowNumber = 1;
             rowNumber <= lastHeaderRow;
             rowNumber++)
        {
            int? articleColumn = null;
            int? stockColumn = null;
            var lastColumn = worksheet.Row(rowNumber)
                .LastCellUsed()?.Address.ColumnNumber ?? 0;

            for (var columnNumber = 1;
                 columnNumber <= lastColumn;
                 columnNumber++)
            {
                var header = NormalizeHeader(
                    worksheet.Cell(rowNumber, columnNumber)
                        .GetFormattedString());

                if (ArticleHeaders.Contains(header, StringComparer.Ordinal))
                {
                    articleColumn = columnNumber;
                }

                if (StockHeaders.Contains(header, StringComparer.Ordinal))
                {
                    stockColumn = columnNumber;
                }
            }

            if (articleColumn.HasValue && stockColumn.HasValue)
            {
                return new HeaderLocation(
                    rowNumber,
                    articleColumn.Value,
                    stockColumn.Value);
            }
        }

        throw new InvalidDataException(
            "Не найдены колонки «Артикул» и «Остаток». Заголовки должны находиться в первых 20 строках.");
    }

    private static bool TryReadQuantity(
        IXLCell cell,
        string formattedValue,
        out decimal quantity)
    {
        if (cell.TryGetValue(out quantity))
        {
            return true;
        }

        var normalizedValue = formattedValue
            .Replace("\u00a0", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal);

        return decimal.TryParse(
                normalizedValue,
                NumberStyles.Number,
                CultureInfo.GetCultureInfo("ru-RU"),
                out quantity)
            || decimal.TryParse(
                normalizedValue,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out quantity);
    }

    private static string NormalizeArticle(string value) =>
        value.Trim().ToUpperInvariant();

    private static string NormalizeHeader(string value) =>
        string.Join(
                ' ',
                value.Trim().ToUpperInvariant()
                    .Split(
                        [' ', '\t', '\r', '\n'],
                        StringSplitOptions.RemoveEmptyEntries))
            .TrimEnd(':');

    private static void AddIssue(
        List<CatalogStockImportIssue> issues,
        int rowNumber,
        string article,
        string message)
    {
        if (issues.Count >= MaximumReportedIssuesCount)
        {
            return;
        }

        issues.Add(new CatalogStockImportIssue(
            rowNumber,
            article,
            message));
    }

    private sealed record HeaderLocation(
        int RowNumber,
        int ArticleColumn,
        int StockColumn);

    private sealed record ParsedStockRow(
        int RowNumber,
        string Article,
        string NormalizedArticle,
        decimal Quantity);

    private sealed record ParsedWorkbookRows(
        IReadOnlyList<ParsedStockRow> Rows,
        IReadOnlyList<CatalogStockImportIssue> Issues,
        int ReadRowsCount,
        int SkippedRowsCount);
}
