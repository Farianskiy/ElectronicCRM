using System.Globalization;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog
    .ImportBatches.Analysis;
using ElectronicService.Domain.Catalog
    .Characteristics;
using ElectronicService.Domain.Catalog
    .ImportBatches;
using ElectronicService.Domain.Catalog
    .ProductTypes;
using ElectronicService.Domain.Common;

namespace ElectronicService.Infrastructure.Postgres
    .Catalog.ImportBatches;

public sealed class CatalogImportWorkbookAnalyzer
    : ICatalogImportWorkbookAnalyzer
{
    private const int MaximumColumns = 200;
    private const int MaximumRows = 20_000;
    private const int HeaderSearchRowsLimit = 20;

    private static readonly JsonSerializerOptions
        JsonOptions =
            new(JsonSerializerDefaults.Web);

    private static readonly Dictionary<
     string,
     CatalogImportColumnTargetKind>
     StandardHeaderMappings =
         new(StringComparer.Ordinal)
         {
             ["НАИМЕНОВАНИЕ"] =
                    CatalogImportColumnTargetKind.Name,

             ["НАЗВАНИЕ"] =
                    CatalogImportColumnTargetKind.Name,

             ["НАИМЕНОВАНИЕ ТОВАРА"] =
                    CatalogImportColumnTargetKind.Name,

             ["НАЗВАНИЕ ТОВАРА"] =
                    CatalogImportColumnTargetKind.Name,

             ["АРТИКУЛ"] =
                    CatalogImportColumnTargetKind.Article,

             ["КОД"] =
                    CatalogImportColumnTargetKind.Article,

             ["КОД ТОВАРА"] =
                    CatalogImportColumnTargetKind.Article,

             ["SKU"] =
                    CatalogImportColumnTargetKind.Article,

             ["ПРОИЗВОДИТЕЛЬ"] =
                    CatalogImportColumnTargetKind.Manufacturer,

             ["БРЕНД"] =
                    CatalogImportColumnTargetKind.Manufacturer,

             ["МАРКА"] =
                    CatalogImportColumnTargetKind.Manufacturer,

             ["ЦЕНА"] =
                    CatalogImportColumnTargetKind.Price,

             ["СТОИМОСТЬ"] =
                    CatalogImportColumnTargetKind.Price,

             ["ЦЕНА РУБ"] =
                    CatalogImportColumnTargetKind.Price,

             ["ОСТАТОК"] =
                    CatalogImportColumnTargetKind.StockQuantity,

             ["КОЛИЧЕСТВО НА СКЛАДЕ"] =
                    CatalogImportColumnTargetKind.StockQuantity,

             ["СКЛАДСКОЙ ОСТАТОК"] =
                    CatalogImportColumnTargetKind.StockQuantity
         };

    private static readonly
        CatalogImportColumnTargetKind[]
        RequiredStandardTargets =
        [
            CatalogImportColumnTargetKind.Name,
            CatalogImportColumnTargetKind.Article,
            CatalogImportColumnTargetKind.Manufacturer
        ];

    public Result<
        CatalogImportWorkbookAnalysis,
        DomainError> Analyze(
            Guid batchId,
            ReadOnlyMemory<byte> workbookContent,
            ProductType? productType,
            IReadOnlyCollection<
                CharacteristicDefinition>
                characteristicDefinitions,
            CancellationToken cancellationToken =
                default)
    {
        if (batchId == Guid.Empty)
        {
            return Result.Failure<
                CatalogImportWorkbookAnalysis,
                DomainError>(
                    GeneralErrors.ValueIsInvalid(
                        nameof(batchId)));
        }

        if (workbookContent.IsEmpty)
        {
            return Result.Failure<
                CatalogImportWorkbookAnalysis,
                DomainError>(
                    CatalogImportErrors
                        .FileIsEmpty());
        }

        ArgumentNullException.ThrowIfNull(
            characteristicDefinitions);

        try
        {
            using var stream =
                new MemoryStream(
                    workbookContent.ToArray(),
                    writable: false);

            using var workbook =
                new XLWorkbook(stream);

            var worksheet =
                workbook.Worksheets
                    .FirstOrDefault(sheet =>
                        sheet.LastCellUsed()
                            is not null);

            if (worksheet is null)
            {
                return Result.Failure<
                    CatalogImportWorkbookAnalysis,
                    DomainError>(
                        CatalogImportErrors
                            .WorkbookHasNoData());
            }

            var lastRowNumber =
                worksheet.LastRowUsed()
                    ?.RowNumber()
                ?? 0;

            var lastColumnNumber =
                worksheet.LastColumnUsed()
                    ?.ColumnNumber()
                ?? 0;

            if (lastRowNumber == 0
                || lastColumnNumber == 0)
            {
                return Result.Failure<
                    CatalogImportWorkbookAnalysis,
                    DomainError>(
                        CatalogImportErrors
                            .WorkbookHasNoData());
            }

            if (lastColumnNumber
                > MaximumColumns)
            {
                return Result.Failure<
                    CatalogImportWorkbookAnalysis,
                    DomainError>(
                        CatalogImportErrors
                            .WorkbookHasTooManyColumns(
                                MaximumColumns));
            }

            var definitionLookups =
                BuildDefinitionLookups(
                    characteristicDefinitions);

            var headerRowNumber =
                FindHeaderRowNumber(
                    worksheet,
                    lastRowNumber,
                    lastColumnNumber,
                    definitionLookups,
                    cancellationToken);

            if (headerRowNumber is null)
            {
                return Result.Failure<
                    CatalogImportWorkbookAnalysis,
                    DomainError>(
                        CatalogImportErrors
                            .WorkbookHasNoHeader());
            }

            var columnCandidates =
                BuildColumnCandidates(
                    worksheet,
                    headerRowNumber.Value,
                    lastRowNumber,
                    lastColumnNumber,
                    definitionLookups,
                    cancellationToken);

            if (columnCandidates.Count == 0)
            {
                return Result.Failure<
                    CatalogImportWorkbookAnalysis,
                    DomainError>(
                        CatalogImportErrors
                            .WorkbookHasNoHeader());
            }

            columnCandidates =
                RemoveAmbiguousDuplicateMappings(
                    columnCandidates);

            var columnsResult =
                CreateDomainColumns(
                    batchId,
                    columnCandidates);

            if (columnsResult.IsFailure)
            {
                return Result.Failure<
                    CatalogImportWorkbookAnalysis,
                    DomainError>(
                        columnsResult.Error);
            }

            var mappingRequired =
                IsMappingRequired(
                    columnCandidates,
                    productType);

            var definitionsById =
                characteristicDefinitions
                    .ToDictionary(
                        definition =>
                            definition.Id);

            var rows = new List<
                CatalogImportRow>();

            var validRowsCount = 0;
            var errorRowsCount = 0;

            for (var rowNumber =
                     headerRowNumber.Value + 1;
                 rowNumber <= lastRowNumber;
                 rowNumber++)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                var rawValues =
                    ReadRawValues(
                        worksheet,
                        rowNumber,
                        columnCandidates);

                if (rawValues.Values.All(
                        string.IsNullOrWhiteSpace))
                {
                    continue;
                }

                if (rows.Count >= MaximumRows)
                {
                    return Result.Failure<
                        CatalogImportWorkbookAnalysis,
                        DomainError>(
                            CatalogImportErrors
                                .WorkbookHasTooManyRows(
                                    MaximumRows));
                }

                var rowBuildResult =
                    BuildRow(
                        rawValues,
                        columnCandidates,
                        productType,
                        definitionsById,
                        mappingRequired);

                var rowResult =
                    CatalogImportRow.Create(
                        batchId,
                        rowNumber,
                        rowBuildResult.Status,
                        JsonSerializer.Serialize(
                            rawValues,
                            JsonOptions),
                        JsonSerializer.Serialize(
                            rowBuildResult.Data,
                            JsonOptions),
                        JsonSerializer.Serialize(
                            rowBuildResult.Issues,
                            JsonOptions),
                        JsonSerializer.Serialize(
                            rowBuildResult.Warnings,
                            JsonOptions));

                if (rowResult.IsFailure)
                {
                    return Result.Failure<
                        CatalogImportWorkbookAnalysis,
                        DomainError>(
                            rowResult.Error);
                }

                rows.Add(rowResult.Value);

                if (rowBuildResult.Status
                    == CatalogImportRowStatus.Valid)
                {
                    validRowsCount++;
                }
                else if (rowBuildResult.Status
                         == CatalogImportRowStatus
                             .Error)
                {
                    errorRowsCount++;
                }
            }

            if (rows.Count == 0)
            {
                return Result.Failure<
                    CatalogImportWorkbookAnalysis,
                    DomainError>(
                        CatalogImportErrors
                            .WorkbookHasNoData());
            }

            return Result.Success<
                CatalogImportWorkbookAnalysis,
                DomainError>(
                    new CatalogImportWorkbookAnalysis(
                        columnsResult.Value,
                        rows,
                        mappingRequired,
                        validRowsCount,
                        errorRowsCount));
        }
        catch (Exception exception)
            when (exception
                  is not OperationCanceledException)
        {
            return Result.Failure<
                CatalogImportWorkbookAnalysis,
                DomainError>(
                    CatalogImportErrors
                        .InvalidWorkbook());
        }
    }

    private static int? FindHeaderRowNumber(
        IXLWorksheet worksheet,
        int lastRowNumber,
        int lastColumnNumber,
        DefinitionLookups definitionLookups,
        CancellationToken cancellationToken)
    {
        var searchLastRow =
            Math.Min(
                lastRowNumber,
                HeaderSearchRowsLimit);

        int? bestRowNumber = null;
        var bestScore = -1;

        for (var rowNumber = 1;
             rowNumber <= searchLastRow;
             rowNumber++)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            var nonEmptyCells = 0;
            var recognizedCells = 0;

            for (var columnNumber = 1;
                 columnNumber <= lastColumnNumber;
                 columnNumber++)
            {
                var header =
                    worksheet
                        .Cell(
                            rowNumber,
                            columnNumber)
                        .GetFormattedString()
                        .Trim();

                if (string.IsNullOrWhiteSpace(
                        header))
                {
                    continue;
                }

                nonEmptyCells++;

                var mapping =
                    ResolveMapping(
                        NormalizeHeader(header),
                        definitionLookups);

                if (mapping.TargetKind
                    != CatalogImportColumnTargetKind
                        .Unmapped)
                {
                    recognizedCells++;
                }
            }

            /*
             * Одна ячейка обычно является
             * заголовком документа, а не таблицы.
             */
            if (nonEmptyCells < 2)
            {
                continue;
            }

            /*
             * Распознанные заголовки имеют
             * значительно больший вес.
             */
            var score =
                recognizedCells * 1_000
                + nonEmptyCells;

            if (score > bestScore)
            {
                bestScore = score;
                bestRowNumber = rowNumber;
            }
        }

        return bestRowNumber;
    }

    private static List<ColumnCandidate>
        BuildColumnCandidates(
            IXLWorksheet worksheet,
            int headerRowNumber,
            int lastRowNumber,
            int lastColumnNumber,
            DefinitionLookups definitionLookups,
            CancellationToken cancellationToken)
    {
        var result =
            new List<ColumnCandidate>();

        for (var columnNumber = 1;
             columnNumber <= lastColumnNumber;
             columnNumber++)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            var sourceHeader =
                worksheet
                    .Cell(
                        headerRowNumber,
                        columnNumber)
                    .GetFormattedString()
                    .Trim();

            if (string.IsNullOrWhiteSpace(
                    sourceHeader)
                && !ColumnHasData(
                    worksheet,
                    columnNumber,
                    headerRowNumber + 1,
                    lastRowNumber))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(
                    sourceHeader))
            {
                sourceHeader =
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"Колонка {columnNumber}");
            }

            var normalizedHeader =
                NormalizeHeader(sourceHeader);

            var mapping =
                ResolveMapping(
                    normalizedHeader,
                    definitionLookups);

            result.Add(
                new ColumnCandidate(
                    columnNumber,
                    sourceHeader,
                    normalizedHeader,
                    mapping.TargetKind,
                    mapping
                        .CharacteristicDefinitionId,
                    mapping.Confidence,
                    mapping.IsConfirmed));
        }

        return result;
    }

    private static bool ColumnHasData(
        IXLWorksheet worksheet,
        int columnNumber,
        int firstDataRowNumber,
        int lastRowNumber)
    {
        for (var rowNumber =
                 firstDataRowNumber;
             rowNumber <= lastRowNumber;
             rowNumber++)
        {
            if (!string.IsNullOrWhiteSpace(
                    worksheet
                        .Cell(
                            rowNumber,
                            columnNumber)
                        .GetFormattedString()))
            {
                return true;
            }
        }

        return false;
    }

    private static ColumnMapping
        ResolveMapping(
            string normalizedHeader,
            DefinitionLookups definitionLookups)
    {
        if (StandardHeaderMappings.TryGetValue(
                normalizedHeader,
                out var standardTarget))
        {
            return new ColumnMapping(
                standardTarget,
                null,
                1.0000m,
                true);
        }

        /*
         * Старые файлы могут содержать:
         *
         * Производитель автоматы
         * Производитель ВРУ
         * Производитель (ЩМП)
         *
         * Но "Производитель серия" не является
         * производителем самого товара.
         */
        if (normalizedHeader.StartsWith(
                "ПРОИЗВОДИТЕЛЬ ",
                StringComparison.Ordinal)
            && !normalizedHeader.Contains(
                "СЕРИЯ",
                StringComparison.Ordinal))
        {
            return new ColumnMapping(
                CatalogImportColumnTargetKind
                    .Manufacturer,
                null,
                0.9000m,
                false);
        }

        if (normalizedHeader.StartsWith(
                "ЦЕНА ",
                StringComparison.Ordinal))
        {
            return new ColumnMapping(
                CatalogImportColumnTargetKind.Price,
                null,
                0.9000m,
                false);
        }

        if (definitionLookups
            .ByNormalizedCode
            .TryGetValue(
                normalizedHeader,
                out var definitionByCode))
        {
            return new ColumnMapping(
                CatalogImportColumnTargetKind
                    .Characteristic,
                definitionByCode.Id,
                1.0000m,
                true);
        }

        if (definitionLookups
            .ByNormalizedName
            .TryGetValue(
                normalizedHeader,
                out var definitionByName))
        {
            return new ColumnMapping(
                CatalogImportColumnTargetKind
                    .Characteristic,
                definitionByName.Id,
                0.9800m,
                true);
        }

        var prefixDefinition =
            FindCharacteristicByPrefix(
                normalizedHeader,
                definitionLookups);

        if (prefixDefinition is not null)
        {
            return new ColumnMapping(
                CatalogImportColumnTargetKind
                    .Characteristic,
                prefixDefinition.Id,
                0.9000m,
                false);
        }

        return new ColumnMapping(
            CatalogImportColumnTargetKind
                .Unmapped,
            null,
            0.0000m,
            false);
    }

    private static CharacteristicDefinition?
        FindCharacteristicByPrefix(
            string normalizedHeader,
            DefinitionLookups definitionLookups)
    {
        var candidates =
            definitionLookups.ByNormalizedName
                .Where(item =>
                    normalizedHeader.StartsWith(
                        item.Key + " ",
                        StringComparison.Ordinal))
                .Select(item =>
                    new
                    {
                        Definition = item.Value,
                        Length = item.Key.Length
                    })
                .Concat(
                    definitionLookups
                        .ByNormalizedCode
                        .Where(item =>
                            normalizedHeader
                                .StartsWith(
                                    item.Key + " ",
                                    StringComparison
                                        .Ordinal))
                        .Select(item =>
                            new
                            {
                                Definition = item.Value,
                                Length = item.Key.Length
                            }))
                .GroupBy(item =>
                    item.Definition.Id)
                .Select(group =>
                    group.OrderByDescending(
                            item => item.Length)
                        .First())
                .OrderByDescending(item =>
                    item.Length)
                .Take(2)
                .ToList();

        if (candidates.Count != 1)
        {
            return null;
        }

        return candidates[0].Definition;
    }

    private static List<ColumnCandidate>
        RemoveAmbiguousDuplicateMappings(
            IReadOnlyCollection<ColumnCandidate>
                candidates)
    {
        var duplicateKeys =
            candidates
                .Select(candidate =>
                    new
                    {
                        Candidate = candidate,
                        Key = GetMappingKey(
                            candidate)
                    })
                .Where(item =>
                    item.Key is not null)
                .GroupBy(
                    item => item.Key!,
                    StringComparer.Ordinal)
                .Where(group =>
                    group.Count() > 1)
                .Select(group => group.Key)
                .ToHashSet(
                    StringComparer.Ordinal);

        return candidates
            .Select(candidate =>
            {
                var key =
                    GetMappingKey(candidate);

                if (key is null
                    || !duplicateKeys.Contains(key))
                {
                    return candidate;
                }

                /*
                 * Например одновременно присутствуют:
                 *
                 * Артикул
                 * Код
                 *
                 * Автоматически выбирать источник
                 * небезопасно. Пользователь решит это
                 * через mapping editor.
                 */
                return candidate with
                {
                    TargetKind =
                        CatalogImportColumnTargetKind
                            .Unmapped,
                    CharacteristicDefinitionId = null,
                    Confidence = 0.0000m,
                    IsConfirmed = false
                };
            })
            .ToList();
    }

    private static string? GetMappingKey(
        ColumnCandidate candidate)
    {
        if (candidate.TargetKind is
            CatalogImportColumnTargetKind.Unmapped
            or CatalogImportColumnTargetKind.Ignore)
        {
            return null;
        }

        if (candidate.TargetKind
            == CatalogImportColumnTargetKind
                .Characteristic)
        {
            return candidate
                .CharacteristicDefinitionId
                ?.ToString();
        }

        return candidate.TargetKind.ToString();
    }

    private static Result<
        IReadOnlyCollection<CatalogImportColumn>,
        DomainError> CreateDomainColumns(
            Guid batchId,
            IReadOnlyCollection<ColumnCandidate>
                candidates)
    {
        var columns =
            new List<CatalogImportColumn>(
                candidates.Count);

        foreach (var candidate in candidates)
        {
            var columnResult =
                CatalogImportColumn.Create(
                    batchId,
                    candidate.SourceColumnNumber,
                    candidate.SourceHeader,
                    candidate.NormalizedSourceHeader,
                    candidate.TargetKind,
                    candidate
                        .CharacteristicDefinitionId,
                    candidate.Confidence,
                    candidate.IsConfirmed);

            if (columnResult.IsFailure)
            {
                return Result.Failure<
                    IReadOnlyCollection<
                        CatalogImportColumn>,
                    DomainError>(
                        columnResult.Error);
            }

            columns.Add(columnResult.Value);
        }

        return Result.Success<
            IReadOnlyCollection<
                CatalogImportColumn>,
            DomainError>(
                columns);
    }

    private static bool IsMappingRequired(
    IReadOnlyCollection<ColumnCandidate>
        candidates,
    ProductType? productType)
    {
        /*
         * Без выбранного типа товара невозможно
         * определить обязательные характеристики.
         */
        if (productType is null)
        {
            return true;
        }

        /*
         * Хотя бы одна неизвестная или
         * неподтверждённая колонка требует
         * ручного сопоставления.
         */
        if (candidates.Any(candidate =>
                candidate.TargetKind
                    == CatalogImportColumnTargetKind
                        .Unmapped
                || !candidate.IsConfirmed))
        {
            return true;
        }

        /*
         * Проверяем обязательные стандартные поля:
         *
         * Name
         * Article
         * Manufacturer
         */
        if (RequiredStandardTargets.Any(
                requiredTarget =>
                    !candidates.Any(candidate =>
                        candidate.TargetKind
                            == requiredTarget
                        && candidate.IsConfirmed)))
        {
            return true;
        }

        /*
         * Проверяем наличие подтверждённой
         * Excel-колонки для каждой обязательной
         * характеристики выбранного типа товара.
         */
        return productType.Characteristics
            .Where(characteristic =>
                characteristic.IsRequired)
            .Select(characteristic =>
                characteristic
                    .CharacteristicDefinitionId)
            .Any(requiredDefinitionId =>
                !candidates.Any(candidate =>
                    candidate.TargetKind
                        == CatalogImportColumnTargetKind
                            .Characteristic
                    && candidate
                        .CharacteristicDefinitionId
                        == requiredDefinitionId
                    && candidate.IsConfirmed));
    }

    private static Dictionary<int, string>
        ReadRawValues(
            IXLWorksheet worksheet,
            int rowNumber,
            IReadOnlyCollection<ColumnCandidate>
                candidates)
    {
        return candidates
            .OrderBy(candidate =>
                candidate.SourceColumnNumber)
            .ToDictionary(
                candidate =>
                    candidate.SourceColumnNumber,
                candidate =>
                    worksheet
                        .Cell(
                            rowNumber,
                            candidate
                                .SourceColumnNumber)
                        .GetFormattedString()
                        .Trim());
    }

    private static RowBuildResult BuildRow(
        Dictionary<int, string> rawValues,
        IReadOnlyCollection<ColumnCandidate>
            candidates,
        ProductType? productType,
        Dictionary<
            Guid,
            CharacteristicDefinition>
            definitionsById,
        bool mappingRequired)
    {
        var issues =
            new List<CatalogImportRowIssue>();

        var warnings =
            new List<CatalogImportRowIssue>();

        var name =
            GetStandardValue(
                CatalogImportColumnTargetKind.Name,
                rawValues,
                candidates);

        var article =
            GetStandardValue(
                CatalogImportColumnTargetKind.Article,
                rawValues,
                candidates);

        var manufacturer =
            GetStandardValue(
                CatalogImportColumnTargetKind
                    .Manufacturer,
                rawValues,
                candidates);

        decimal? price = null;
        int? stockQuantity = null;

        var rawPrice =
            GetStandardValue(
                CatalogImportColumnTargetKind.Price,
                rawValues,
                candidates);

        if (!string.IsNullOrWhiteSpace(
                rawPrice))
        {
            if (TryParseDecimal(
                    rawPrice,
                    out var parsedPrice)
                && parsedPrice >= 0)
            {
                price = parsedPrice;
            }
            else
            {
                issues.Add(
                    CreateIssue(
                        "price.invalid",
                        "Цена должна быть " +
                        "неотрицательным числом.",
                        "price",
                        FindColumnNumber(
                            CatalogImportColumnTargetKind
                                .Price,
                            candidates)));
            }
        }

        var rawStock =
            GetStandardValue(
                CatalogImportColumnTargetKind
                    .StockQuantity,
                rawValues,
                candidates);

        if (!string.IsNullOrWhiteSpace(
                rawStock))
        {
            if (TryParseInteger(
                    rawStock,
                    out var parsedStock)
                && parsedStock >= 0)
            {
                stockQuantity = parsedStock;
            }
            else
            {
                issues.Add(
                    CreateIssue(
                        "stock.invalid",
                        "Остаток должен быть " +
                        "неотрицательным целым числом.",
                        "stockQuantity",
                        FindColumnNumber(
                            CatalogImportColumnTargetKind
                                .StockQuantity,
                            candidates)));
            }
        }

        var characteristics =
            new Dictionary<string, string>(
                StringComparer.Ordinal);

        foreach (var candidate in candidates
                     .Where(candidate =>
                         candidate.TargetKind
                         == CatalogImportColumnTargetKind
                             .Characteristic))
        {
            if (candidate
                    .CharacteristicDefinitionId
                is not Guid definitionId)
            {
                continue;
            }

            if (!definitionsById.TryGetValue(
                    definitionId,
                    out var definition))
            {
                continue;
            }

            rawValues.TryGetValue(
                candidate.SourceColumnNumber,
                out var rawValue);

            if (string.IsNullOrWhiteSpace(
                    rawValue))
            {
                continue;
            }

            if (TryNormalizeCharacteristicValue(
                    rawValue,
                    definition,
                    out var normalizedValue))
            {
                characteristics[
                    definitionId.ToString()] =
                        normalizedValue;
            }
            else
            {
                issues.Add(
                    CreateIssue(
                        "characteristic.invalid",
                        $"Значение характеристики " +
                        $"'{definition.Name}' не " +
                        $"соответствует типу " +
                        $"'{definition.DataType}'.",
                        definitionId.ToString(),
                        candidate
                            .SourceColumnNumber));
            }
        }

        if (!mappingRequired)
        {
            AddRequiredValueIssues(
                name,
                article,
                manufacturer,
                candidates,
                issues);

            AddRequiredCharacteristicIssues(
                productType!,
                rawValues,
                candidates,
                definitionsById,
                issues);
        }

        CatalogImportRowStatus status;

        if (mappingRequired)
        {
            status =
                CatalogImportRowStatus
                    .PendingMapping;
        }
        else if (issues.Count == 0)
        {
            status =
                CatalogImportRowStatus.Valid;
        }
        else
        {
            status =
                CatalogImportRowStatus.Error;
        }

        return new RowBuildResult(
            status,
            new CatalogImportNormalizedRowData(
                NormalizeNullable(name),
                NormalizeNullable(article),
                NormalizeNullable(manufacturer),
                price,
                stockQuantity,
                characteristics),
            issues,
            warnings);
    }

    private static void AddRequiredValueIssues(
        string? name,
        string? article,
        string? manufacturer,
        IReadOnlyCollection<ColumnCandidate>
            candidates,
        List<CatalogImportRowIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            issues.Add(
                CreateIssue(
                    "name.required",
                    "Не указано наименование товара.",
                    "name",
                    FindColumnNumber(
                        CatalogImportColumnTargetKind
                            .Name,
                        candidates)));
        }

        if (string.IsNullOrWhiteSpace(article))
        {
            issues.Add(
                CreateIssue(
                    "article.required",
                    "Не указан артикул товара.",
                    "article",
                    FindColumnNumber(
                        CatalogImportColumnTargetKind
                            .Article,
                        candidates)));
        }

        if (string.IsNullOrWhiteSpace(
                manufacturer))
        {
            issues.Add(
                CreateIssue(
                    "manufacturer.required",
                    "Не указан производитель товара.",
                    "manufacturer",
                    FindColumnNumber(
                        CatalogImportColumnTargetKind
                            .Manufacturer,
                        candidates)));
        }
    }

    private static void
        AddRequiredCharacteristicIssues(
            ProductType productType,
            Dictionary<int, string> rawValues,
            IReadOnlyCollection<ColumnCandidate>
                candidates,
            Dictionary<
                Guid,
                CharacteristicDefinition>
                definitionsById,
            List<CatalogImportRowIssue> issues)
    {
        var requiredIds =
            productType.Characteristics
                .Where(characteristic =>
                    characteristic.IsRequired)
                .Select(characteristic =>
                    characteristic
                        .CharacteristicDefinitionId);

        foreach (var definitionId
                 in requiredIds)
        {
            var candidate =
                candidates.FirstOrDefault(
                    candidate =>
                        candidate.TargetKind
                        == CatalogImportColumnTargetKind
                            .Characteristic
                        && candidate
                            .CharacteristicDefinitionId
                        == definitionId);

            if (candidate is null)
            {
                /*
                 * Отсутствующая колонка уже должна
                 * перевести batch в MappingRequired.
                 */
                continue;
            }

            rawValues.TryGetValue(
                candidate.SourceColumnNumber,
                out var rawValue);

            if (!string.IsNullOrWhiteSpace(
                    rawValue))
            {
                continue;
            }

            var displayName =
                definitionsById.TryGetValue(
                    definitionId,
                    out var definition)
                    ? definition.Name
                    : definitionId.ToString();

            issues.Add(
                CreateIssue(
                    "characteristic.required",
                    $"Не заполнена обязательная " +
                    $"характеристика " +
                    $"'{displayName}'.",
                    definitionId.ToString(),
                    candidate.SourceColumnNumber));
        }
    }

    private static string? GetStandardValue(
        CatalogImportColumnTargetKind targetKind,
        Dictionary<int, string> rawValues,
        IReadOnlyCollection<ColumnCandidate>
            candidates)
    {
        var candidate =
            candidates.FirstOrDefault(
                candidate =>
                    candidate.TargetKind
                    == targetKind);

        if (candidate is null)
        {
            return null;
        }

        return rawValues.TryGetValue(
            candidate.SourceColumnNumber,
            out var value)
                ? value
                : null;
    }

    private static int? FindColumnNumber(
        CatalogImportColumnTargetKind targetKind,
        IReadOnlyCollection<ColumnCandidate>
            candidates)
    {
        return candidates
            .FirstOrDefault(candidate =>
                candidate.TargetKind
                == targetKind)
            ?.SourceColumnNumber;
    }

    private static CatalogImportRowIssue
        CreateIssue(
            string code,
            string message,
            string? field,
            int? sourceColumnNumber)
    {
        return new CatalogImportRowIssue(
            code,
            message,
            field,
            sourceColumnNumber);
    }

    private static bool
        TryNormalizeCharacteristicValue(
            string rawValue,
            CharacteristicDefinition definition,
            out string normalizedValue)
    {
        switch (definition.DataType)
        {
            case CharacteristicDataType.Text:
                normalizedValue =
                    rawValue.Trim();

                return normalizedValue.Length > 0;

            case CharacteristicDataType.Number:
                if (TryParseDecimal(
                        rawValue,
                        out var number))
                {
                    normalizedValue =
                        number.ToString(
                            CultureInfo.InvariantCulture);

                    return true;
                }

                break;

            case CharacteristicDataType.Boolean:
                if (TryParseBoolean(
                        rawValue,
                        out var boolean))
                {
                    normalizedValue =
                        boolean
                            ? "true"
                            : "false";

                    return true;
                }

                break;
        }

        normalizedValue = string.Empty;
        return false;
    }

    private static bool TryParseDecimal(
        string rawValue,
        out decimal value)
    {
        var source =
            rawValue
                .Trim()
                .Replace(
                    '\u00A0',
                    ' ')
                .Replace(
                    ",",
                    ".",
                    StringComparison.Ordinal);

        var numberBuilder =
            new StringBuilder();

        var numberStarted = false;
        var decimalSeparatorAdded = false;

        foreach (var character in source)
        {
            if (char.IsDigit(character))
            {
                numberBuilder.Append(character);
                numberStarted = true;
                continue;
            }

            if ((character == '-'
                 || character == '+')
                && !numberStarted
                && numberBuilder.Length == 0)
            {
                numberBuilder.Append(character);
                continue;
            }

            if (character == '.'
                && numberStarted
                && !decimalSeparatorAdded)
            {
                numberBuilder.Append(character);
                decimalSeparatorAdded = true;
                continue;
            }

            if (numberStarted)
            {
                break;
            }
        }

        return decimal.TryParse(
            numberBuilder.ToString(),
            NumberStyles.AllowLeadingSign
            | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out value);
    }

    private static bool TryParseInteger(
        string rawValue,
        out int value)
    {
        value = 0;

        if (!TryParseDecimal(
                rawValue,
                out var decimalValue))
        {
            return false;
        }

        if (decimalValue
            != decimal.Truncate(decimalValue))
        {
            return false;
        }

        if (decimalValue < int.MinValue
            || decimalValue > int.MaxValue)
        {
            return false;
        }

        value = decimal.ToInt32(decimalValue);
        return true;
    }

    private static bool TryParseBoolean(
        string rawValue,
        out bool value)
    {
        var normalized =
            NormalizeHeader(rawValue)
                .Replace(
                    " ",
                    string.Empty,
                    StringComparison.Ordinal);

        switch (normalized)
        {
            case "ДА":
            case "YES":
            case "TRUE":
            case "1":
            case "+":
            case "ЕСТЬ":
            case "ИМЕЕТСЯ":
                value = true;
                return true;

            case "НЕТ":
            case "NO":
            case "FALSE":
            case "0":
            case "-":
            case "ОТСУТСТВУЕТ":
                value = false;
                return true;

            default:
                value = false;
                return false;
        }
    }

    private static DefinitionLookups
        BuildDefinitionLookups(
            IReadOnlyCollection<
                CharacteristicDefinition>
                definitions)
    {
        return new DefinitionLookups(
            BuildUniqueDefinitionLookup(
                definitions,
                definition =>
                    NormalizeHeader(
                        definition.Code)),
            BuildUniqueDefinitionLookup(
                definitions,
                definition =>
                    NormalizeHeader(
                        definition.Name)));
    }

    private static Dictionary<
        string,
        CharacteristicDefinition>
        BuildUniqueDefinitionLookup(
            IReadOnlyCollection<
                CharacteristicDefinition>
                definitions,
            Func<
                CharacteristicDefinition,
                string> keySelector)
    {
        return definitions
            .GroupBy(
                keySelector,
                StringComparer.Ordinal)
            .Where(group =>
                !string.IsNullOrWhiteSpace(
                    group.Key)
                && group.Count() == 1)
            .ToDictionary(
                group => group.Key,
                group => group.Single(),
                StringComparer.Ordinal);
    }

    private static string NormalizeHeader(
        string value)
    {
        var builder =
            new StringBuilder(value.Length);

        var previousWasSpace = false;

        foreach (var originalCharacter
                 in value.Trim())
        {
            var character =
                originalCharacter == 'ё'
                    || originalCharacter == 'Ё'
                    ? 'Е'
                    : char.ToUpperInvariant(
                        originalCharacter);

            if (char.IsLetterOrDigit(character)
                || character == '+'
                || character == '-')
            {
                builder.Append(character);
                previousWasSpace = false;
            }
            else if (!previousWasSpace
                     && builder.Length > 0)
            {
                builder.Append(' ');
                previousWasSpace = true;
            }
        }

        return builder
            .ToString()
            .Trim();
    }

    private static string? NormalizeNullable(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private sealed record DefinitionLookups(
        IReadOnlyDictionary<
            string,
            CharacteristicDefinition>
            ByNormalizedCode,
        IReadOnlyDictionary<
            string,
            CharacteristicDefinition>
            ByNormalizedName);

    private sealed record ColumnMapping(
        CatalogImportColumnTargetKind TargetKind,
        Guid? CharacteristicDefinitionId,
        decimal Confidence,
        bool IsConfirmed);

    private sealed record ColumnCandidate(
        int SourceColumnNumber,
        string SourceHeader,
        string NormalizedSourceHeader,
        CatalogImportColumnTargetKind TargetKind,
        Guid? CharacteristicDefinitionId,
        decimal Confidence,
        bool IsConfirmed);

    private sealed record RowBuildResult(
        CatalogImportRowStatus Status,
        CatalogImportNormalizedRowData Data,
        IReadOnlyCollection<
            CatalogImportRowIssue> Issues,
        IReadOnlyCollection<
            CatalogImportRowIssue> Warnings);
}