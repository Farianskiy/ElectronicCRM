using ClosedXML.Excel;
using ElectronicService.Core.Catalog.Products.ExportProducts;
using ElectronicService.Core.Catalog.Products.GetProducts;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.ProductTypes;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Products;

public sealed class CatalogProductsWorkbookExporter
    : ICatalogProductsWorkbookExporter
{
    private const int BaseColumnsCount = 16;

    private static readonly XLColor HeaderColor =
        XLColor.FromHtml("#0F766E");

    private static readonly XLColor SummaryColor =
        XLColor.FromHtml("#E6FFFB");

    private readonly ElectronicDbContext _dbContext;

    public CatalogProductsWorkbookExporter(ElectronicDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public async Task<byte[]> ExportAsync(
        IReadOnlyCollection<CatalogProductListItemResult> products,
        DateTime generatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(products);

        var productIds = products
            .Select(product => product.Id)
            .Distinct()
            .ToArray();

        var details = await LoadProductDetailsAsync(
                productIds,
                cancellationToken)
            .ConfigureAwait(false);

        var characteristics = await LoadCharacteristicsAsync(
                productIds,
                cancellationToken)
            .ConfigureAwait(false);

        using var workbook = new XLWorkbook();

        WriteProductsWorksheet(
            workbook,
            products,
            details,
            characteristics);

        WriteSummaryWorksheet(
            workbook,
            products,
            generatedAtUtc);

        WriteMetadataWorksheet(
            workbook,
            generatedAtUtc,
            characteristics);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return stream.ToArray();
    }

    private async Task<IReadOnlyDictionary<Guid, ProductExportDetails>>
        LoadProductDetailsAsync(
            Guid[] productIds,
            CancellationToken cancellationToken)
    {
        if (productIds.Length == 0)
        {
            return new Dictionary<Guid, ProductExportDetails>();
        }

        var rows = await (
                from product in _dbContext.Products
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                join productType in _dbContext.ProductTypes.AsNoTracking()
                    on product.ProductTypeId equals productType.Id
                where productIds.Contains(product.Id)
                select new ProductExportDetails(
                    product.Id,
                    productType.Kind,
                    product.CreatedAtUtc,
                    product.UpdatedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows.ToDictionary(row => row.ProductId);
    }

    private async Task<IReadOnlyList<ProductCharacteristicExportValue>>
        LoadCharacteristicsAsync(
            Guid[] productIds,
            CancellationToken cancellationToken)
    {
        if (productIds.Length == 0)
        {
            return [];
        }

        return await (
                from characteristic in _dbContext.ProductCharacteristics
                    .AsNoTracking()
                join definition in _dbContext.CharacteristicDefinitions
                        .AsNoTracking()
                    on characteristic.CharacteristicDefinitionId
                    equals definition.Id
                where productIds.Contains(characteristic.ProductId)
                select new ProductCharacteristicExportValue(
                    characteristic.ProductId,
                    definition.Id,
                    definition.Code,
                    definition.Name,
                    definition.DataType,
                    definition.Unit,
                    characteristic.Value.TextValue,
                    characteristic.Value.NumberValue,
                    characteristic.Value.BooleanValue))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static void WriteSummaryWorksheet(
        XLWorkbook workbook,
        IReadOnlyCollection<CatalogProductListItemResult> products,
        DateTime generatedAtUtc)
    {
        var worksheet = workbook.Worksheets.Add("Сводка");
        worksheet.ShowGridLines = false;

        worksheet.Cell("A1").Value = "Выгрузка каталога товаров";
        worksheet.Range("A1:B1").Merge();
        ConfigureTitle(worksheet.Range("A1:B1"));

        worksheet.Cell("A3").Value = "Выгружено";
        worksheet.Cell("B3").Value = generatedAtUtc;
        worksheet.Cell("B3").Style.DateFormat.Format = "dd.MM.yyyy HH:mm";
        worksheet.Cell("A4").Value = "Товаров";
        worksheet.Cell("B4").Value = products.Count;
        worksheet.Cell("A5").Value = "Активных";
        worksheet.Cell("B5").Value = products.Count(product => !product.IsArchived);
        worksheet.Cell("A6").Value = "Архивных";
        worksheet.Cell("B6").Value = products.Count(product => product.IsArchived);
        worksheet.Cell("A7").Value = "Производителей";
        worksheet.Cell("B7").Value = products
            .Select(product => product.ManufacturerId)
            .Distinct()
            .Count();
        worksheet.Cell("A8").Value = "Типов товаров";
        worksheet.Cell("B8").Value = products
            .Select(product => product.ProductTypeCode)
            .Distinct(StringComparer.Ordinal)
            .Count();

        worksheet.Range("A3:A8").Style.Font.Bold = true;
        worksheet.Range("A3:B8").Style.Fill.BackgroundColor = SummaryColor;
        worksheet.Column(1).Width = 24;
        worksheet.Column(2).Width = 24;
    }

    private static void WriteProductsWorksheet(
        XLWorkbook workbook,
        IReadOnlyCollection<CatalogProductListItemResult> products,
        IReadOnlyDictionary<Guid, ProductExportDetails> details,
        IReadOnlyList<ProductCharacteristicExportValue> characteristics)
    {
        var worksheet = workbook.Worksheets.Add("Товары");
        worksheet.ShowGridLines = false;

        var characteristicColumns = characteristics
            .GroupBy(
                characteristic => characteristic.DefinitionId)
            .Select(group => group.First())
            .OrderBy(characteristic => characteristic.Name,
                StringComparer.OrdinalIgnoreCase)
            .ThenBy(characteristic => characteristic.Code,
                StringComparer.Ordinal)
            .ToArray();

        var headers = new[]
        {
            "ID товара",
            "Артикул",
            "Наименование",
            "Производитель",
            "ID производителя",
            "Тип товара",
            "Код типа товара",
            "Назначение",
            "Текущая цена",
            "Валюта",
            "Базовая цена",
            "Базовая валюта",
            "Остаток",
            "Состояние",
            "Создан",
            "Изменён"
        };

        for (var index = 0; index < headers.Length; index++)
        {
            worksheet.Cell(1, index + 1).Value = headers[index];
        }

        for (var index = 0; index < characteristicColumns.Length; index++)
        {
            var characteristic = characteristicColumns[index];
            var label = string.IsNullOrWhiteSpace(characteristic.Unit)
                ? characteristic.Name
                : $"{characteristic.Name}, {characteristic.Unit}";

            worksheet.Cell(1, BaseColumnsCount + index + 1).Value =
                ToSafeExcelText(label);
        }

        var lastColumn = BaseColumnsCount + characteristicColumns.Length;
        ConfigureHeader(worksheet.Range(1, 1, 1, lastColumn));

        var characteristicLookup = characteristics
            .ToDictionary(
                characteristic => (
                    characteristic.ProductId,
                    characteristic.DefinitionId));

        var currentRow = 2;

        foreach (var product in products)
        {
            details.TryGetValue(product.Id, out var productDetails);

            worksheet.Cell(currentRow, 1).Value = product.Id.ToString();
            worksheet.Cell(currentRow, 2).Value = ToSafeExcelText(product.Article);
            worksheet.Cell(currentRow, 3).Value = ToSafeExcelText(product.Name);
            worksheet.Cell(currentRow, 4).Value = ToSafeExcelText(product.ManufacturerName);
            worksheet.Cell(currentRow, 5).Value = product.ManufacturerId.ToString();
            worksheet.Cell(currentRow, 6).Value = ToSafeExcelText(product.ProductTypeName);
            worksheet.Cell(currentRow, 7).Value = ToSafeExcelText(product.ProductTypeCode);
            worksheet.Cell(currentRow, 8).Value = GetProductTypeKindLabel(productDetails?.Kind);
            worksheet.Cell(currentRow, 9).Value = product.PriceAmount;
            worksheet.Cell(currentRow, 10).Value = ToSafeExcelText(product.PriceCurrency);
            worksheet.Cell(currentRow, 11).Value = product.BasePriceAmount;
            worksheet.Cell(currentRow, 12).Value = ToSafeExcelText(product.BasePriceCurrency);
            worksheet.Cell(currentRow, 13).Value = product.StockQuantity;
            worksheet.Cell(currentRow, 14).Value = product.IsArchived
                ? "Архивный"
                : "Активный";

            if (productDetails is not null)
            {
                worksheet.Cell(currentRow, 15).Value = productDetails.CreatedAtUtc;

                if (productDetails.UpdatedAtUtc.HasValue)
                {
                    worksheet.Cell(currentRow, 16).Value =
                        productDetails.UpdatedAtUtc.Value;
                }
            }

            for (var index = 0; index < characteristicColumns.Length; index++)
            {
                var definition = characteristicColumns[index];

                if (!characteristicLookup.TryGetValue(
                        (product.Id, definition.DefinitionId),
                        out var value))
                {
                    continue;
                }

                WriteCharacteristicValue(
                    worksheet.Cell(
                        currentRow,
                        BaseColumnsCount + index + 1),
                    value);
            }

            currentRow++;
        }

        worksheet.Range(1, 1, Math.Max(1, currentRow - 1), lastColumn)
            .SetAutoFilter();
        worksheet.SheetView.FreezeRows(1);
        worksheet.Row(1).Height = 30;
        worksheet.Column(1).Hide();
        worksheet.Column(5).Hide();
        worksheet.Column(2).Style.NumberFormat.Format = "@";
        worksheet.Column(9).Style.NumberFormat.Format = "0.####";
        worksheet.Column(11).Style.NumberFormat.Format = "0.####";
        worksheet.Column(13).Style.NumberFormat.Format = "0.####";
        worksheet.Columns(15, 16).Style.DateFormat.Format = "dd.MM.yyyy HH:mm";

        worksheet.Column(2).Width = 18;
        worksheet.Column(3).Width = 36;
        worksheet.Column(4).Width = 18;
        worksheet.Column(6).Width = 24;
        worksheet.Column(7).Width = 20;
        worksheet.Column(8).Width = 18;
        worksheet.Columns(9, 13).Width = 14;
        worksheet.Column(14).Width = 14;
        worksheet.Columns(15, 16).Width = 20;

        if (characteristicColumns.Length > 0)
        {
            worksheet.Columns(BaseColumnsCount + 1, lastColumn).Width = 18;
        }

        if (currentRow > 2)
        {
            var dataRange = worksheet.Range(
                2,
                1,
                currentRow - 1,
                lastColumn);

            dataRange.Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Center;
            dataRange.Style.Alignment.WrapText = false;
            worksheet.Rows(2, currentRow - 1).Height = 20;
        }
    }

    private static void WriteMetadataWorksheet(
        XLWorkbook workbook,
        DateTime generatedAtUtc,
        IReadOnlyList<ProductCharacteristicExportValue> characteristics)
    {
        var worksheet = workbook.Worksheets.Add("_Справочник");

        worksheet.Cell("A1").Value = "Format";
        worksheet.Cell("B1").Value = "ElectronicCRM.ProductCatalog";
        worksheet.Cell("A2").Value = "Version";
        worksheet.Cell("B2").Value = 1;
        worksheet.Cell("A3").Value = "GeneratedAtUtc";
        worksheet.Cell("B3").Value = generatedAtUtc;
        worksheet.Cell("B3").Style.DateFormat.Format = "yyyy-MM-dd HH:mm:ss";

        worksheet.Cell("A5").Value = "ID характеристики";
        worksheet.Cell("B5").Value = "Код";
        worksheet.Cell("C5").Value = "Название";
        worksheet.Cell("D5").Value = "Тип данных";
        worksheet.Cell("E5").Value = "Единица";
        ConfigureHeader(worksheet.Range("A5:E5"));

        var currentRow = 6;

        foreach (var characteristic in characteristics
                     .GroupBy(value => value.DefinitionId)
                     .Select(group => group.First())
                     .OrderBy(value => value.Name,
                         StringComparer.OrdinalIgnoreCase))
        {
            worksheet.Cell(currentRow, 1).Value =
                characteristic.DefinitionId.ToString();
            worksheet.Cell(currentRow, 2).Value =
                ToSafeExcelText(characteristic.Code);
            worksheet.Cell(currentRow, 3).Value =
                ToSafeExcelText(characteristic.Name);
            worksheet.Cell(currentRow, 4).Value =
                characteristic.DataType.ToString();
            worksheet.Cell(currentRow, 5).Value =
                ToSafeExcelText(characteristic.Unit ?? string.Empty);
            currentRow++;
        }

        worksheet.Columns(1, 5).AdjustToContents();
        worksheet.Visibility = XLWorksheetVisibility.VeryHidden;
    }

    private static void WriteCharacteristicValue(
        IXLCell cell,
        ProductCharacteristicExportValue characteristic)
    {
        switch (characteristic.DataType)
        {
            case CharacteristicDataType.Number
                when characteristic.NumberValue.HasValue:
                cell.Value = characteristic.NumberValue.Value;
                cell.Style.NumberFormat.Format = "#,##0.####";
                break;

            case CharacteristicDataType.Boolean
                when characteristic.BooleanValue.HasValue:
                cell.Value = characteristic.BooleanValue.Value
                    ? "Да"
                    : "Нет";
                break;

            default:
                cell.Value = ToSafeExcelText(
                    characteristic.TextValue ?? string.Empty);
                break;
        }
    }

    private static string GetProductTypeKindLabel(ProductTypeKind? kind)
    {
        return kind switch
        {
            ProductTypeKind.MainProduct => "Основной товар",
            ProductTypeKind.Component => "Комплектующее",
            _ => string.Empty
        };
    }

    private static void ConfigureTitle(IXLRange range)
    {
        range.Style.Fill.BackgroundColor = HeaderColor;
        range.Style.Font.FontColor = XLColor.White;
        range.Style.Font.Bold = true;
        range.Style.Font.FontSize = 16;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }

    private static void ConfigureHeader(IXLRange range)
    {
        range.Style.Fill.BackgroundColor = HeaderColor;
        range.Style.Font.FontColor = XLColor.White;
        range.Style.Font.Bold = true;
        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        range.Style.Alignment.WrapText = true;
    }

    private static string ToSafeExcelText(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value[0] is '=' or '+' or '-' or '@'
            ? $"'{value}"
            : value;
    }

    private sealed record ProductExportDetails(
        Guid ProductId,
        ProductTypeKind Kind,
        DateTime CreatedAtUtc,
        DateTime? UpdatedAtUtc);

    private sealed record ProductCharacteristicExportValue(
        Guid ProductId,
        Guid DefinitionId,
        string Code,
        string Name,
        CharacteristicDataType DataType,
        string? Unit,
        string? TextValue,
        decimal? NumberValue,
        bool? BooleanValue);
}
