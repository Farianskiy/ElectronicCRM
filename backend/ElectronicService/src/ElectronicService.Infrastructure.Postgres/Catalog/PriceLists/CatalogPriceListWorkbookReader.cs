using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;
using CSharpFunctionalExtensions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using ElectronicService.Core.Catalog.PriceLists.Import;
using ElectronicService.Domain.Catalog.PriceLists;
using ElectronicService.Domain.Common;

namespace ElectronicService.Infrastructure.Postgres.Catalog.PriceLists;

public sealed class CatalogPriceListWorkbookReader
    : ICatalogPriceListWorkbookReader
{
    private const string PriceWorksheetName = "Прайс";

    private const int HeaderSearchRowsLimit = 30;

    private static readonly CultureInfo RussianCulture =
        CultureInfo.GetCultureInfo("ru-RU");

    public async Task<
        Result<
            CatalogPriceListWorkbookReadSummary,
            DomainError>> ReadAsync(
                string originalFileName,
                ReadOnlyMemory<byte> fileContent,
                Func<
                    CatalogPriceListSourceRow,
                    CancellationToken,
                    Task<UnitResult<DomainError>>> consumeRowAsync,
                    Func<
                        CatalogPriceListWorkbookReadProgress,
                        CancellationToken,
                        Task<UnitResult<DomainError>>> reportProgressAsync,
                CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            return Result.Failure<
                CatalogPriceListWorkbookReadSummary,
                DomainError>(
                    GeneralErrors.ValueIsRequired(
                        nameof(originalFileName)));
        }

        if (fileContent.IsEmpty)
        {
            return Result.Failure<
                CatalogPriceListWorkbookReadSummary,
                DomainError>(
                    CatalogPriceListErrors.FileIsEmpty());
        }

        ArgumentNullException.ThrowIfNull(
            consumeRowAsync);

        ArgumentNullException.ThrowIfNull(
            reportProgressAsync);

        var extractionResult =
            await CatalogPriceListWorkbookExtractor
                .ExtractAsync(
                    originalFileName,
                    fileContent,
                    cancellationToken)
                .ConfigureAwait(false);

        if (extractionResult.IsFailure)
        {
            return Result.Failure<
                CatalogPriceListWorkbookReadSummary,
                DomainError>(
                    extractionResult.Error);
        }

        try
        {
            using var workbookStream =
                new MemoryStream(
                    extractionResult.Value.ToArray(),
                    writable: false);

            using var document =
                SpreadsheetDocument.Open(
                    workbookStream,
                    isEditable: false);

            return await ReadDocumentAsync(
                    document,
                    consumeRowAsync,
                    reportProgressAsync,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OpenXmlPackageException)
        {
            return Result.Failure<
                CatalogPriceListWorkbookReadSummary,
                DomainError>(
                    CatalogPriceListErrors.InvalidWorkbook());
        }
        catch (InvalidOperationException exception)
            when (string.Equals(
                exception.Message,
                "Specified part does not exist in the package.",
                StringComparison.Ordinal))
        {
            return Result.Failure<
                CatalogPriceListWorkbookReadSummary,
                DomainError>(
                    CatalogPriceListErrors.InvalidWorkbook());
        }
        catch (InvalidDataException)
        {
            return Result.Failure<
                CatalogPriceListWorkbookReadSummary,
                DomainError>(
                    CatalogPriceListErrors.InvalidWorkbook());
        }
        catch (IOException)
        {
            return Result.Failure<
                CatalogPriceListWorkbookReadSummary,
                DomainError>(
                    CatalogPriceListErrors.InvalidWorkbook());
        }
        catch (XmlException)
        {
            return Result.Failure<
                CatalogPriceListWorkbookReadSummary,
                DomainError>(
                    CatalogPriceListErrors.InvalidWorkbook());
        }
    }

    private static async Task<
        Result<
            CatalogPriceListWorkbookReadSummary,
            DomainError>> ReadDocumentAsync(
                SpreadsheetDocument document,
                Func<
                    CatalogPriceListSourceRow,
                    CancellationToken,
                    Task<UnitResult<DomainError>>> consumeRowAsync,
                Func<
                    CatalogPriceListWorkbookReadProgress,
                    CancellationToken,
                    Task<UnitResult<DomainError>>> reportProgressAsync,
                CancellationToken cancellationToken)
    {
        var workbookPart =
            document.WorkbookPart;

        if (workbookPart is null)
        {
            return Result.Failure<
                CatalogPriceListWorkbookReadSummary,
                DomainError>(
                    CatalogPriceListErrors.InvalidWorkbook());
        }

        var worksheetResult =
            FindPriceWorksheet(workbookPart);

        if (worksheetResult.IsFailure)
        {
            return Result.Failure<
                CatalogPriceListWorkbookReadSummary,
                DomainError>(
                    worksheetResult.Error);
        }

        var sharedStrings =
            ReadSharedStrings(workbookPart);

        return await ReadWorksheetAsync(
                worksheetResult.Value,
                sharedStrings,
                consumeRowAsync,
                reportProgressAsync,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static Result<
    WorksheetPart,
    DomainError> FindPriceWorksheet(
        WorkbookPart workbookPart)
    {
        const string TransitionalRelationshipNamespace =
            "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        const string StrictRelationshipNamespace =
            "http://purl.oclc.org/ooxml/officeDocument/relationships";

        string? relationshipId = null;

        var settings =
            new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                IgnoreComments = true,
                IgnoreWhitespace = true
            };

        using var workbookStream =
            workbookPart.GetStream(
                FileMode.Open,
                FileAccess.Read);

        using var reader =
            XmlReader.Create(
                workbookStream,
                settings);

        while (reader.Read())
        {
            if (reader.NodeType
                    != XmlNodeType.Element
                || !string.Equals(
                    reader.LocalName,
                    "sheet",
                    StringComparison.Ordinal))
            {
                continue;
            }

            var sheetName =
                reader.GetAttribute("name");

            if (!string.Equals(
                    sheetName,
                    PriceWorksheetName,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            relationshipId =
                reader.GetAttribute(
                    "id",
                    TransitionalRelationshipNamespace)
                ?? reader.GetAttribute(
                    "id",
                    StrictRelationshipNamespace);

            break;
        }

        if (string.IsNullOrWhiteSpace(
                relationshipId))
        {
            return Result.Failure<
                WorksheetPart,
                DomainError>(
                    CatalogPriceListErrors
                        .PriceWorksheetNotFound(
                            PriceWorksheetName));
        }

        var worksheetPart =
            workbookPart.GetPartById(
                relationshipId)
            as WorksheetPart;

        if (worksheetPart is null)
        {
            return Result.Failure<
                WorksheetPart,
                DomainError>(
                    CatalogPriceListErrors
                        .PriceWorksheetNotFound(
                            PriceWorksheetName));
        }

        return Result.Success<
            WorksheetPart,
            DomainError>(
                worksheetPart);
    }

    private static string[]
    ReadSharedStrings(
        WorkbookPart workbookPart)
    {
        var sharedStringTablePart =
            workbookPart.SharedStringTablePart;

        if (sharedStringTablePart is null)
        {
            return [];
        }

        var settings =
            new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                IgnoreComments = true,
                IgnoreWhitespace = false
            };

        var values =
            new List<string>();

        using var sharedStringsStream =
            sharedStringTablePart.GetStream(
                FileMode.Open,
                FileAccess.Read);

        using var reader =
            XmlReader.Create(
                sharedStringsStream,
                settings);

        while (reader.Read())
        {
            if (reader.NodeType
                    != XmlNodeType.Element
                || !string.Equals(
                    reader.LocalName,
                    "si",
                    StringComparison.Ordinal))
            {
                continue;
            }

            var valueBuilder =
                new StringBuilder();

            using var itemReader =
                reader.ReadSubtree();

            while (itemReader.Read())
            {
                if (itemReader.NodeType
                        != XmlNodeType.Element
                    || !string.Equals(
                        itemReader.LocalName,
                        "t",
                        StringComparison.Ordinal))
                {
                    continue;
                }

                valueBuilder.Append(
                    itemReader.ReadElementContentAsString());
            }

            values.Add(
                valueBuilder.ToString());
        }

        return values.ToArray();
    }

    private static async Task<
        Result<
            CatalogPriceListWorkbookReadSummary,
            DomainError>> ReadWorksheetAsync(
                WorksheetPart worksheetPart,
                string[] sharedStrings,
                Func<
                    CatalogPriceListSourceRow,
                    CancellationToken,
                    Task<UnitResult<DomainError>>> consumeRowAsync,
                Func<
                    CatalogPriceListWorkbookReadProgress,
                    CancellationToken,
                    Task<UnitResult<DomainError>>> reportProgressAsync,
                CancellationToken cancellationToken)
    {
        DateOnly? effectiveDate = null;
        int? headerRowNumber = null;
        var rowsCount = 0;
        var estimatedRowsCount = 0;
        var lastWorksheetRowNumber = 0;

        using var reader =
            OpenXmlReader.Create(
                worksheetPart);

        while (reader.Read())
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            if (!reader.IsStartElement)
            {
                continue;
            }

            if (reader.ElementType
                == typeof(SheetDimension))
            {
                var dimensionElement =
                    reader.LoadCurrentElement();

                if (dimensionElement
                    is SheetDimension sheetDimension)
                {
                    lastWorksheetRowNumber =
                        GetLastRowNumber(
                            sheetDimension.Reference?.Value);
                }

                continue;
            }

            if (reader.ElementType != typeof(Row))
            {
                continue;
            }

            var currentElement =
                reader.LoadCurrentElement();

            if (currentElement is not Row row)
            {
                continue;
            }

            var rowNumber =
                GetRowNumber(row);

            if (rowNumber <= 0)
            {
                continue;
            }

            var cellValues =
                ReadRequiredCellValues(
                    row,
                    sharedStrings);

            if (rowNumber == 2)
            {
                effectiveDate =
                    ParseEffectiveDate(
                        GetValue(
                            cellValues,
                            "B"));
            }

            if (headerRowNumber is null)
            {
                if (IsHeaderRow(cellValues))
                {
                    headerRowNumber = rowNumber;

                    estimatedRowsCount =
                        Math.Max(
                            0,
                            lastWorksheetRowNumber
                            - headerRowNumber.Value);

                    var initialProgressResult =
                        await reportProgressAsync(
                                new CatalogPriceListWorkbookReadProgress(
                                    estimatedRowsCount,
                                    0),
                                cancellationToken)
                            .ConfigureAwait(false);

                    if (initialProgressResult.IsFailure)
                    {
                        return Result.Failure<
                            CatalogPriceListWorkbookReadSummary,
                            DomainError>(
                                initialProgressResult.Error);
                    }

                    continue;
                }

                if (rowNumber
                    >= HeaderSearchRowsLimit)
                {
                    return Result.Failure<
                        CatalogPriceListWorkbookReadSummary,
                        DomainError>(
                            CatalogPriceListErrors
                                .WorkbookHeaderNotFound());
                }

                continue;
            }

            if (rowNumber
                <= headerRowNumber.Value)
            {
                continue;
            }

            var article =
                GetValue(
                    cellValues,
                    "A");

            var name =
                GetValue(
                    cellValues,
                    "B");

            if (string.IsNullOrWhiteSpace(article)
                && string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            rowsCount++;

            if (rowsCount
                > CatalogPriceList.MaximumRowsCount)
            {
                return Result.Failure<
                    CatalogPriceListWorkbookReadSummary,
                    DomainError>(
                        CatalogPriceListErrors
                            .WorkbookRowsLimitExceeded(
                                CatalogPriceList
                                    .MaximumRowsCount));
            }

            var sourceRow =
                new CatalogPriceListSourceRow(
                    rowNumber,
                    article.Trim(),
                    name.Trim(),
                    ParseNullableDecimal(
                        GetValue(
                            cellValues,
                            "H")),
                    ParseNullableDecimal(
                        GetValue(
                            cellValues,
                            "J")),
                    NormalizeOptionalValue(
                        GetValue(
                            cellValues,
                            "C")),
                    NormalizeOptionalValue(
                        GetValue(
                            cellValues,
                            "D")));

            var consumeResult =
                await consumeRowAsync(
                        sourceRow,
                        cancellationToken)
                    .ConfigureAwait(false);

            if (consumeResult.IsFailure)
            {
                return Result.Failure<
                    CatalogPriceListWorkbookReadSummary,
                    DomainError>(
                        consumeResult.Error);
            }

            if (rowsCount % 500 == 0)
            {
                var progressResult =
                    await reportProgressAsync(
                            new CatalogPriceListWorkbookReadProgress(
                                Math.Max(
                                    estimatedRowsCount,
                                    rowsCount),
                                rowsCount),
                            cancellationToken)
                        .ConfigureAwait(false);

                if (progressResult.IsFailure)
                {
                    return Result.Failure<
                        CatalogPriceListWorkbookReadSummary,
                        DomainError>(
                            progressResult.Error);
                }
            }
        }

        if (headerRowNumber is null)
        {
            return Result.Failure<
                CatalogPriceListWorkbookReadSummary,
                DomainError>(
                    CatalogPriceListErrors
                        .WorkbookHeaderNotFound());
        }

        if (effectiveDate is null)
        {
            return Result.Failure<
                CatalogPriceListWorkbookReadSummary,
                DomainError>(
                    CatalogPriceListErrors
                        .EffectiveDateNotFound());
        }

        if (rowsCount == 0)
        {
            return Result.Failure<
                CatalogPriceListWorkbookReadSummary,
                DomainError>(
                    CatalogPriceListErrors
                        .WorkbookHasNoRows());
        }

        var finalProgressResult =
            await reportProgressAsync(
                    new CatalogPriceListWorkbookReadProgress(
                        Math.Max(
                            estimatedRowsCount,
                            rowsCount),
                        rowsCount),
                    cancellationToken)
                .ConfigureAwait(false);

        if (finalProgressResult.IsFailure)
        {
            return Result.Failure<
                CatalogPriceListWorkbookReadSummary,
                DomainError>(
                    finalProgressResult.Error);
        }

        return Result.Success<
            CatalogPriceListWorkbookReadSummary,
            DomainError>(
                new CatalogPriceListWorkbookReadSummary(
                    PriceWorksheetName,
                    effectiveDate.Value,
                    headerRowNumber.Value,
                    rowsCount));
    }

    private static Dictionary<string, string> ReadRequiredCellValues(
            Row row,
            string[] sharedStrings)
    {
        var values =
            new Dictionary<string, string>(
                StringComparer.Ordinal);

        foreach (var cell
                 in row.Elements<Cell>())
        {
            var columnName =
                GetColumnName(cell);

            if (!IsRequiredColumn(
                    columnName))
            {
                continue;
            }

            values[columnName] =
                ReadCellValue(
                    cell,
                    sharedStrings);
        }

        return values;
    }

    private static bool IsRequiredColumn(
        string columnName)
    {
        return columnName is
            "A"
            or "B"
            or "C"
            or "D"
            or "H"
            or "J";
    }

    private static string GetColumnName(
        Cell cell)
    {
        var cellReference =
            cell.CellReference?.Value;

        if (string.IsNullOrWhiteSpace(
                cellReference))
        {
            return string.Empty;
        }

        var lettersCount = 0;

        while (lettersCount
            < cellReference.Length
            && char.IsLetter(
                cellReference[lettersCount]))
        {
            lettersCount++;
        }

        if (lettersCount == 0)
        {
            return string.Empty;
        }

        return cellReference[..lettersCount]
            .ToUpperInvariant();
    }

    private static int GetRowNumber(
        Row row)
    {
        var rowIndex =
            row.RowIndex?.Value;

        if (!rowIndex.HasValue
            || rowIndex.Value > int.MaxValue)
        {
            return 0;
        }

        return (int)rowIndex.Value;
    }

    private static string ReadCellValue(
        Cell cell,
        string[] sharedStrings)
    {
        var rawValue =
            cell.CellValue?.InnerText
            ?? cell.InnerText
            ?? string.Empty;

        if (cell.DataType?.Value
            == CellValues.SharedString)
        {
            if (!int.TryParse(
                    rawValue,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var sharedStringIndex))
            {
                return string.Empty;
            }

            if (sharedStringIndex < 0
                || sharedStringIndex
                    >= sharedStrings.Length)
            {
                return string.Empty;
            }

            return sharedStrings[
                sharedStringIndex];
        }

        if (cell.DataType?.Value
            == CellValues.InlineString)
        {
            return cell.InlineString?
                .InnerText
                ?? cell.InnerText
                ?? string.Empty;
        }

        return rawValue;
    }

    private static bool IsHeaderRow(
        IReadOnlyDictionary<string, string> values)
    {
        var articleHeader =
            NormalizeHeader(
                GetValue(
                    values,
                    "A"));

        var nameHeader =
            NormalizeHeader(
                GetValue(
                    values,
                    "B"));

        return string.Equals(
                articleHeader,
                "АРТИКУЛ",
                StringComparison.Ordinal)
            && (string.Equals(
                    nameHeader,
                    "НАИМЕНОВАНИЕ",
                    StringComparison.Ordinal)
                || nameHeader.Contains(
                    "НАИМЕНОВАНИЕ",
                    StringComparison.Ordinal));
    }

    private static string NormalizeHeader(
        string value)
    {
        return value
            .Trim()
            .ToUpperInvariant()
            .Replace(
                "Ё",
                "Е",
                StringComparison.Ordinal);
    }

    private static DateOnly? ParseEffectiveDate(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalizedValue =
            value.Trim();

        if (double.TryParse(
                normalizedValue,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var oaDate)
            && oaDate is >= 0d
                and <= 2958465d)
        {
            return DateOnly.FromDateTime(
                DateTime.FromOADate(oaDate));
        }

        if (DateTime.TryParse(
                normalizedValue,
                RussianCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out var russianDate))
        {
            return DateOnly.FromDateTime(
                russianDate);
        }

        if (DateTime.TryParse(
                normalizedValue,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out var invariantDate))
        {
            return DateOnly.FromDateTime(
                invariantDate);
        }

        return null;
    }

    private static decimal? ParseNullableDecimal(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalizedValue =
            value
                .Replace(
                    "\u00A0",
                    string.Empty,
                    StringComparison.Ordinal)
                .Trim();

        if (decimal.TryParse(
                normalizedValue,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var invariantValue))
        {
            return invariantValue;
        }

        if (decimal.TryParse(
                normalizedValue,
                NumberStyles.Number,
                RussianCulture,
                out var russianValue))
        {
            return russianValue;
        }

        return null;
    }

    private static string GetValue(
        IReadOnlyDictionary<string, string> values,
        string columnName)
    {
        return values.TryGetValue(
            columnName,
            out var value)
            ? value
            : string.Empty;
    }

    private static string? NormalizeOptionalValue(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static int GetLastRowNumber(
    string? rangeReference)
    {
        if (string.IsNullOrWhiteSpace(
                rangeReference))
        {
            return 0;
        }

        var separatorIndex =
            rangeReference.LastIndexOf(':');

        var lastCellReference =
            separatorIndex >= 0
                ? rangeReference[
                    (separatorIndex + 1)..]
                : rangeReference;

        var firstDigitIndex =
            -1;

        for (var index = 0;
             index < lastCellReference.Length;
             index++)
        {
            if (!char.IsDigit(
                    lastCellReference[index]))
            {
                continue;
            }

            firstDigitIndex = index;

            break;
        }

        if (firstDigitIndex < 0)
        {
            return 0;
        }

        return int.TryParse(
            lastCellReference[firstDigitIndex..],
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out var rowNumber)
                ? rowNumber
                : 0;
    }
}