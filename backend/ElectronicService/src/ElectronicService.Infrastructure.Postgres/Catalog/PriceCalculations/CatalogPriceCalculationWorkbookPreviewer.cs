using System.Globalization;
using ClosedXML.Excel;
using ElectronicService.Core.Catalog.PriceCalculations.Import;
using ElectronicService.Domain.Catalog.PriceCalculations;
using ElectronicService.Domain.Catalog.PriceLists;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.PriceCalculations;

public sealed class CatalogPriceCalculationWorkbookPreviewer
    : ICatalogPriceCalculationWorkbookPreviewer
{
    private const int MaximumRowsCount = 10_000;

    private static readonly string[] ArticleHeaders =
        ["АРТИКУЛ", "ARTICLE", "SKU"];

    private static readonly string[] QuantityHeaders =
        [
            "КОЛИЧЕСТВО",
            "КОЛ-ВО",
            "КОЛ.",
            "QUANTITY",
            "QTY"
        ];

    private static readonly string[] NameHeaders =
        [
            "НАИМЕНОВАНИЕ",
            "НАЗВАНИЕ",
            "ТОВАР",
            "NAME",
            "PRODUCT"
        ];

    private static readonly string[] ManufacturerHeaders =
        [
            "ПРОИЗВОДИТЕЛЬ",
            "БРЕНД",
            "МАРКА",
            "MANUFACTURER",
            "BRAND"
        ];

    private readonly ElectronicDbContext _dbContext;

    public CatalogPriceCalculationWorkbookPreviewer(
        ElectronicDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public async Task<CatalogPriceCalculationImportPreview>
        PreviewAsync(
            Stream workbookStream,
            string fileName,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workbookStream);

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
            var worksheet =
                workbook.Worksheets.FirstOrDefault()
                ?? throw new InvalidDataException(
                    "В файле нет листов.");

            var header = FindHeader(worksheet);

            var lastRowNumber =
                worksheet.LastRowUsed()?.RowNumber()
                ?? header.RowNumber;

            if (lastRowNumber - header.RowNumber
                > MaximumRowsCount)
            {
                throw new InvalidDataException(
                    $"Файл содержит больше {MaximumRowsCount:N0} строк.");
            }

            var parsedWorkbook =
                ParseRows(
                    worksheet,
                    header,
                    lastRowNumber);

            if (parsedWorkbook.ValidRows.Count == 0)
            {
                return CreatePreview(
                    parsedWorkbook.ReadRowsCount,
                    parsedWorkbook.InvalidRows);
            }

            var normalizedArticles =
                parsedWorkbook.ValidRows
                    .Select(row => row.NormalizedArticle)
                    .Distinct(StringComparer.Ordinal)
                    .ToHashSet(StringComparer.Ordinal);

            var allProductReferences =
                await (
                    from product in
                        _dbContext.Products.AsNoTracking()
                    join manufacturer in
                        _dbContext.Manufacturers.AsNoTracking()
                        on product.ManufacturerId
                        equals manufacturer.Id
                    select new ProductReference(
                        product.Id,
                        product.Article.Value,
                        product.Name.Value,
                        product.ManufacturerId,
                        manufacturer.Name,
                        product.StockQuantity.Value))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var productReferences =
                allProductReferences
                    .Where(
                        product =>
                            normalizedArticles.Contains(
                                NormalizeArticle(
                                    product.Article)))
                    .ToArray();

            var productsByArticle =
                productReferences
                    .GroupBy(
                        product =>
                            NormalizeArticle(
                                product.Article),
                        StringComparer.Ordinal)
                    .ToDictionary(
                        group => group.Key,
                        group => group.ToArray(),
                        StringComparer.Ordinal);

            var productIds =
                productReferences
                    .Select(product => product.ProductId)
                    .Distinct()
                    .ToArray();

            var priceSources =
                productIds.Length == 0
                    ? []
                    : await (
                        from row in
                            _dbContext.CatalogPriceListRows
                                .AsNoTracking()
                        join priceList in
                            _dbContext.CatalogPriceLists
                                .AsNoTracking()
                            on row.PriceListId
                            equals priceList.Id
                        where row.ProductId.HasValue
                              && productIds.Contains(
                                  row.ProductId.Value)
                              && priceList.Status
                                  == CatalogPriceListStatus.Active
                              && row.Status
                                  == CatalogPriceListRowStatus.Valid
                              && row.BasePriceAmount.HasValue
                        select new PriceReference(
                            row.ProductId.GetValueOrDefault(),
                            priceList.Id,
                            row.Id,
                            row.BasePriceAmount.GetValueOrDefault(),
                            row.MrcPriceAmount))
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

            var pricesByProduct =
                priceSources
                    .GroupBy(price => price.ProductId)
                    .ToDictionary(
                        group => group.Key,
                        group => group.ToArray());

            var previewRows =
                parsedWorkbook.InvalidRows.ToList();

            foreach (var row in parsedWorkbook.ValidRows)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!productsByArticle.TryGetValue(
                        row.NormalizedArticle,
                        out var articleProducts))
                {
                    previewRows.Add(
                        CreateProblemRow(
                            row,
                            CatalogPriceCalculationImportRowStatus
                                .ProductNotFound,
                            "Товар с таким артикулом не найден."));
                    continue;
                }

                var matchingProducts =
                    FilterByManufacturer(
                        articleProducts,
                        row.SourceManufacturer);

                if (matchingProducts.Length == 0)
                {
                    previewRows.Add(
                        CreateProblemRow(
                            row,
                            CatalogPriceCalculationImportRowStatus
                                .ProductNotFound,
                            "Товар с таким артикулом у указанного производителя не найден."));
                    continue;
                }

                if (matchingProducts.Length > 1)
                {
                    previewRows.Add(
                        CreateProblemRow(
                            row,
                            CatalogPriceCalculationImportRowStatus
                                .ProductAmbiguous,
                            "Найдено несколько товаров. Укажите производителя точнее."));
                    continue;
                }

                var product = matchingProducts[0];

                if (!pricesByProduct.TryGetValue(
                        product.ProductId,
                        out var productPrices)
                    || productPrices.Length == 0)
                {
                    previewRows.Add(
                        CreateProductProblemRow(
                            row,
                            product,
                            CatalogPriceCalculationImportRowStatus
                                .ActivePriceNotFound,
                            "Для товара не найдена цена в активном прайс-листе."));
                    continue;
                }

                if (productPrices.Length > 1)
                {
                    previewRows.Add(
                        CreateProductProblemRow(
                            row,
                            product,
                            CatalogPriceCalculationImportRowStatus
                                .ActivePriceAmbiguous,
                            "Для товара найдено несколько цен в активном прайс-листе."));
                    continue;
                }

                var price = productPrices[0];

                var shortageQuantity =
                    Math.Max(
                        0m,
                        row.Quantity
                        - product.StockQuantity);

                var message =
                    NamesDiffer(
                        row.SourceName,
                        product.Name)
                        ? "Товар сопоставлен по артикулу, но наименование в Excel отличается от каталога."
                        : null;

                previewRows.Add(
                    new CatalogPriceCalculationImportPreviewRow(
                        row.RowNumber,
                        row.Article,
                        row.SourceName,
                        row.SourceManufacturer,
                        row.Quantity,
                        CatalogPriceCalculationImportRowStatus
                            .Matched,
                        message,
                        product.ProductId,
                        product.Article,
                        product.Name,
                        product.ManufacturerId,
                        product.ManufacturerName,
                        product.StockQuantity,
                        shortageQuantity,
                        price.PriceListId,
                        price.PriceListRowId,
                        price.BasePriceAmount,
                        price.MrcPriceAmount));
            }

            return CreatePreview(
                parsedWorkbook.ReadRowsCount,
                previewRows);
        }
    }

    private static CatalogPriceCalculationImportPreview
        CreatePreview(
            int readRowsCount,
            IReadOnlyCollection<
                CatalogPriceCalculationImportPreviewRow> rows)
    {
        var orderedRows =
            rows
                .OrderBy(row => row.RowNumber)
                .ToArray();

        var matchedRowsCount =
            orderedRows.Count(
                row =>
                    row.Status
                    == CatalogPriceCalculationImportRowStatus
                        .Matched);

        return new CatalogPriceCalculationImportPreview(
            readRowsCount,
            matchedRowsCount,
            readRowsCount - matchedRowsCount,
            orderedRows);
    }

    private static ParsedWorkbook ParseRows(
        IXLWorksheet worksheet,
        HeaderLocation header,
        int lastRowNumber)
    {
        var validRows =
            new List<ParsedProjectRow>();

        var invalidRows =
            new List<
                CatalogPriceCalculationImportPreviewRow>();

        var readRowsCount = 0;

        for (var rowNumber = header.RowNumber + 1;
             rowNumber <= lastRowNumber;
             rowNumber++)
        {
            var row = worksheet.Row(rowNumber);

            var article =
                row.Cell(header.ArticleColumn)
                    .GetFormattedString()
                    .Trim();

            var quantityCell =
                row.Cell(header.QuantityColumn);

            var quantityText =
                quantityCell
                    .GetFormattedString()
                    .Trim();

            var sourceName =
                ReadOptionalCell(
                    row,
                    header.NameColumn);

            var sourceManufacturer =
                ReadOptionalCell(
                    row,
                    header.ManufacturerColumn);

            if (article.Length == 0
                && quantityText.Length == 0
                && sourceName is null
                && sourceManufacturer is null)
            {
                continue;
            }

            readRowsCount++;

            if (article.Length == 0)
            {
                invalidRows.Add(
                    CreateInvalidRow(
                        rowNumber,
                        article,
                        sourceName,
                        sourceManufacturer,
                        null,
                        "Не указан артикул."));
                continue;
            }

            if (!TryReadQuantity(
                    quantityCell,
                    quantityText,
                    out var quantity)
                || quantity <= 0m)
            {
                invalidRows.Add(
                    CreateInvalidRow(
                        rowNumber,
                        article,
                        sourceName,
                        sourceManufacturer,
                        null,
                        "Количество должно быть числом больше нуля."));
                continue;
            }

            if (quantity
                > CatalogPriceCalculationLine
                    .MaximumQuantity)
            {
                invalidRows.Add(
                    CreateInvalidRow(
                        rowNumber,
                        article,
                        sourceName,
                        sourceManufacturer,
                        quantity,
                        $"Количество не может превышать {CatalogPriceCalculationLine.MaximumQuantity:N0}."));
                continue;
            }

            validRows.Add(
                new ParsedProjectRow(
                    rowNumber,
                    article,
                    NormalizeArticle(article),
                    sourceName,
                    sourceManufacturer,
                    quantity));
        }

        return new ParsedWorkbook(
            readRowsCount,
            validRows,
            invalidRows);
    }

    private static HeaderLocation FindHeader(
        IXLWorksheet worksheet)
    {
        var lastHeaderRow =
            Math.Min(
                worksheet.LastRowUsed()
                    ?.RowNumber()
                ?? 1,
                20);

        for (var rowNumber = 1;
             rowNumber <= lastHeaderRow;
             rowNumber++)
        {
            int? articleColumn = null;
            int? quantityColumn = null;
            int? nameColumn = null;
            int? manufacturerColumn = null;

            var lastColumn =
                worksheet.Row(rowNumber)
                    .LastCellUsed()
                    ?.Address.ColumnNumber
                ?? 0;

            for (var columnNumber = 1;
                 columnNumber <= lastColumn;
                 columnNumber++)
            {
                var value =
                    NormalizeHeader(
                        worksheet
                            .Cell(
                                rowNumber,
                                columnNumber)
                            .GetFormattedString());

                if (ArticleHeaders.Contains(
                        value,
                        StringComparer.Ordinal))
                {
                    articleColumn = columnNumber;
                }

                if (QuantityHeaders.Contains(
                        value,
                        StringComparer.Ordinal))
                {
                    quantityColumn = columnNumber;
                }

                if (NameHeaders.Contains(
                        value,
                        StringComparer.Ordinal))
                {
                    nameColumn = columnNumber;
                }

                if (ManufacturerHeaders.Contains(
                        value,
                        StringComparer.Ordinal))
                {
                    manufacturerColumn = columnNumber;
                }
            }

            if (articleColumn.HasValue
                && quantityColumn.HasValue)
            {
                return new HeaderLocation(
                    rowNumber,
                    articleColumn.Value,
                    quantityColumn.Value,
                    nameColumn,
                    manufacturerColumn);
            }
        }

        throw new InvalidDataException(
            "Не найдены колонки «Артикул» и «Количество». Заголовки должны находиться в первых 20 строках.");
    }

    private static ProductReference[] FilterByManufacturer(
        IReadOnlyCollection<ProductReference> products,
        string? sourceManufacturer)
    {
        if (string.IsNullOrWhiteSpace(
                sourceManufacturer))
        {
            return products.ToArray();
        }

        var normalizedManufacturer =
            NormalizeText(sourceManufacturer);

        return products
            .Where(
                product =>
                    string.Equals(
                        NormalizeText(
                            product.ManufacturerName),
                        normalizedManufacturer,
                        StringComparison.Ordinal))
            .ToArray();
    }

    private static string? ReadOptionalCell(
        IXLRow row,
        int? columnNumber)
    {
        if (!columnNumber.HasValue)
        {
            return null;
        }

        var value =
            row.Cell(columnNumber.Value)
                .GetFormattedString()
                .Trim();

        return value.Length == 0
            ? null
            : value;
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

        var normalizedValue =
            formattedValue
                .Replace(
                    "\u00a0",
                    string.Empty,
                    StringComparison.Ordinal)
                .Replace(
                    " ",
                    string.Empty,
                    StringComparison.Ordinal);

        return decimal.TryParse(
                   normalizedValue,
                   NumberStyles.Number,
                   CultureInfo.GetCultureInfo(
                       "ru-RU"),
                   out quantity)
               || decimal.TryParse(
                   normalizedValue,
                   NumberStyles.Number,
                   CultureInfo.InvariantCulture,
                   out quantity);
    }

    private static bool NamesDiffer(
        string? sourceName,
        string productName)
    {
        return !string.IsNullOrWhiteSpace(
                   sourceName)
               && !string.Equals(
                   NormalizeText(sourceName),
                   NormalizeText(productName),
                   StringComparison.Ordinal);
    }

    private static string NormalizeArticle(
        string value)
    {
        return value
            .Trim()
            .ToUpperInvariant();
    }

    private static string NormalizeText(
        string value)
    {
        return string.Join(
                ' ',
                value
                    .Trim()
                    .ToUpperInvariant()
                    .Replace(
                        "Ё",
                        "Е",
                        StringComparison.Ordinal)
                    .Split(
                        [' ', '\t', '\r', '\n'],
                        StringSplitOptions
                            .RemoveEmptyEntries))
            .Trim();
    }

    private static string NormalizeHeader(
        string value)
    {
        return NormalizeText(value)
            .TrimEnd(':');
    }

    private static CatalogPriceCalculationImportPreviewRow
        CreateInvalidRow(
            int rowNumber,
            string article,
            string? sourceName,
            string? sourceManufacturer,
            decimal? quantity,
            string message)
    {
        return new CatalogPriceCalculationImportPreviewRow(
            rowNumber,
            article,
            sourceName,
            sourceManufacturer,
            quantity,
            CatalogPriceCalculationImportRowStatus.Invalid,
            message,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);
    }

    private static CatalogPriceCalculationImportPreviewRow
        CreateProblemRow(
            ParsedProjectRow row,
            CatalogPriceCalculationImportRowStatus status,
            string message)
    {
        return new CatalogPriceCalculationImportPreviewRow(
            row.RowNumber,
            row.Article,
            row.SourceName,
            row.SourceManufacturer,
            row.Quantity,
            status,
            message,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);
    }

    private static CatalogPriceCalculationImportPreviewRow
        CreateProductProblemRow(
            ParsedProjectRow row,
            ProductReference product,
            CatalogPriceCalculationImportRowStatus status,
            string message)
    {
        var shortageQuantity =
            Math.Max(
                0m,
                row.Quantity
                - product.StockQuantity);

        return new CatalogPriceCalculationImportPreviewRow(
            row.RowNumber,
            row.Article,
            row.SourceName,
            row.SourceManufacturer,
            row.Quantity,
            status,
            message,
            product.ProductId,
            product.Article,
            product.Name,
            product.ManufacturerId,
            product.ManufacturerName,
            product.StockQuantity,
            shortageQuantity,
            null,
            null,
            null,
            null);
    }

    private sealed record HeaderLocation(
        int RowNumber,
        int ArticleColumn,
        int QuantityColumn,
        int? NameColumn,
        int? ManufacturerColumn);

    private sealed record ParsedProjectRow(
        int RowNumber,
        string Article,
        string NormalizedArticle,
        string? SourceName,
        string? SourceManufacturer,
        decimal Quantity);

    private sealed record ParsedWorkbook(
        int ReadRowsCount,
        IReadOnlyList<ParsedProjectRow> ValidRows,
        IReadOnlyList<
            CatalogPriceCalculationImportPreviewRow> InvalidRows);

    private sealed record ProductReference(
        Guid ProductId,
        string Article,
        string Name,
        Guid ManufacturerId,
        string ManufacturerName,
        decimal StockQuantity);

    private sealed record PriceReference(
        Guid ProductId,
        Guid PriceListId,
        Guid PriceListRowId,
        decimal BasePriceAmount,
        decimal? MrcPriceAmount);
}