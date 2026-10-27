using System.Globalization;
using ClosedXML.Excel;
using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Components;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.Components;
using ElectronicService.Domain.Catalog.ProductTypes;
using ElectronicService.Domain.Catalog.ValueObjects;
using ElectronicService.Domain.Common;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Components;

public sealed class CatalogComponentCompatibilityWorkbookService
    : ICatalogComponentCompatibilityWorkbookService
{
    private const int MaximumRowsCount = 5_000;
    private readonly ElectronicDbContext _dbContext;

    public CatalogComponentCompatibilityWorkbookService(
        ElectronicDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<byte[], DomainError>> CreateTemplateAsync(
        string? mainProductTypeCode,
        IReadOnlyCollection<string>? characteristicCodes = null,
        IReadOnlyCollection<string>? rangeCharacteristicCodes = null,
        CancellationToken cancellationToken = default)
    {
        ProductTypeReference? selectedType = null;
        IReadOnlyCollection<CharacteristicReference> selectedCharacteristics = [];

        if (!string.IsNullOrWhiteSpace(mainProductTypeCode))
        {
            var normalizedType = mainProductTypeCode.Trim().ToUpperInvariant();
            selectedType = await _dbContext.ProductTypes
                .AsNoTracking()
                .Where(item => item.Kind == ProductTypeKind.MainProduct
                    && item.Code == normalizedType)
                .Select(item => new ProductTypeReference(
                    item.Id,
                    item.Code,
                    item.Name))
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (selectedType is null)
            {
                return Failure<byte[]>(
                    "Выбранный тип основного товара не найден.");
            }

            selectedCharacteristics = await (
                    from relation in _dbContext.ProductTypeCharacteristics
                        .AsNoTracking()
                    join definition in _dbContext.CharacteristicDefinitions
                        .AsNoTracking()
                        on relation.CharacteristicDefinitionId equals definition.Id
                    where relation.ProductTypeId == selectedType.Id
                    orderby definition.Name
                    select new CharacteristicReference(
                        relation.ProductTypeId,
                        definition.Id,
                        definition.Code,
                        definition.Name,
                        definition.DataType))
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);

            if (characteristicCodes is not null)
            {
                var selectedCodes = characteristicCodes
                    .Select(Normalize)
                    .ToHashSet(StringComparer.Ordinal);
                selectedCharacteristics = selectedCharacteristics
                    .Where(item => selectedCodes.Contains(Normalize(item.Code)))
                    .ToArray();
            }
        }

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Правила совместимости");
        var headers = new List<string>
        {
            "Артикул комплектующего",
            "Наименование комплектующего",
            "Производитель комплектующего",
            "Тип основного товара",
            "Потребность",
            "Активно"
        };
        var selectedExportCharacteristics = selectedCharacteristics
            .Select(item => new CharacteristicExportReference(
                item.DefinitionId,
                item.Code,
                item.Name,
                item.DataType))
            .ToArray();
        var selectedRangeCodes = (rangeCharacteristicCodes ?? [])
            .Select(Normalize)
            .ToHashSet(StringComparer.Ordinal);
        var selectedRangeDefinitionIds = selectedExportCharacteristics
            .Where(item => item.DataType == CharacteristicDataType.Number
                && selectedRangeCodes.Contains(Normalize(item.Code)))
            .Select(item => item.Id)
            .ToHashSet();
        var characteristicColumns = CreateCharacteristicColumns(
            selectedExportCharacteristics,
            selectedRangeDefinitionIds);
        headers.AddRange(characteristicColumns.Count > 0
            ? characteristicColumns.Select(item => item.Header)
            : ["Характеристика 1", "Характеристика 2"]);

        for (var index = 0; index < headers.Count; index++)
        {
            worksheet.Cell(1, index + 1).Value = headers[index];
        }

        worksheet.Cell(2, 1).Value = "АРТИКУЛ-001";
        worksheet.Cell(2, 2).Value = "Наименование из каталога (необязательно)";
        worksheet.Cell(2, 3).Value = "Производитель";
        worksheet.Cell(2, 4).Value = selectedType?.Code ?? "КОНТАКТОР";
        worksheet.Cell(2, 5).Value = string.Empty;
        worksheet.Cell(2, 6).Value = "Да";
        if (headers.Count > 6)
        {
            worksheet.Cell(2, 7).Value = "значение; другое значение";
        }
        StyleRulesWorksheet(worksheet, headers.Count);

        AddProductsWorksheet(workbook);

        var instructions = workbook.Worksheets.Add("Инструкция");
        instructions.Cell(1, 1).Value = "Как заполнить шаблон";
        instructions.Cell(1, 1).Style.Font.Bold = true;
        instructions.Cell(3, 1).Value =
            "1. Комплектующее должно уже существовать в каталоге либо быть добавлено в этом же файле на листе «Товары».";
        instructions.Cell(4, 1).Value =
            "2. Наименование комплектующего заполняется для удобства. Производитель вместе с артикулом однозначно определяет товар и защищает от совпадающих артикулов разных брендов.";
        instructions.Cell(5, 1).Value =
            "3. Тип основного товара можно указать кодом или названием.";
        instructions.Cell(6, 1).Value =
            "4. Оставьте «Потребность» пустой для обычного совместимого аксессуара. Укажите код или название только для обязательной сборки, количества или отдельной группы выбора.";
        instructions.Cell(7, 1).Value =
            selectedType is null
                ? "5. Переименуйте колонки «Характеристика 1/2» в реальные коды или названия характеристик."
                : "5. Колонки характеристик уже взяты из схемы выбранного типа товара.";
        instructions.Cell(8, 1).Value =
            "6. Несколько допустимых значений одной характеристики разделяйте точкой с запятой.";
        instructions.Cell(9, 1).Value =
            "7. Повторите артикул, тип и ту же потребность (либо снова оставьте её пустой) в новой строке, чтобы добавить альтернативный вариант совместимости.";
        instructions.Cell(10, 1).Value =
            "8. В одной строке разные характеристики работают одновременно (И); повторные строки работают как варианты (ИЛИ).";
        instructions.Cell(11, 1).Value =
            "9. В колонке «Активно» укажите «Да» или «Нет». Если колонка отсутствует, правило считается активным.";
        instructions.Cell(12, 1).Value =
            "10. Для числового диапазона заполните колонки «характеристика от» и «характеристика до». Границы включаются в диапазон.";
        instructions.Cell(13, 1).Value =
            "11. Если товара или комплектующего ещё нет в каталоге, сначала заполните его на листе «Товары». После применения импорта каталога этот же файл можно проверить на вкладке правил совместимости.";
        instructions.Cell(14, 1).Value =
            "12. На листе «Товары» один и тот же артикул разрешён у разных производителей. Пара «Производитель + Артикул» должна быть уникальной.";
        instructions.Cell(15, 1).Value =
            "13. Пустые условия означают совместимость со всеми товарами выбранного типа. Перед применением такого правила система потребует отдельное подтверждение.";
        instructions.Column(1).Width = 120;
        instructions.Column(1).Style.Alignment.WrapText = true;

        if (selectedType is not null)
        {
            AddMetadataWorksheet(
                workbook,
                selectedType,
                characteristicColumns);
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<Result<byte[], DomainError>> ExportRulesAsync(
        string mainProductTypeCode,
        IReadOnlyCollection<Guid>? componentManufacturerIds = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mainProductTypeCode))
        {
            return Failure<byte[]>("Выберите тип основного товара.");
        }

        var normalizedTypeCode = mainProductTypeCode.Trim().ToUpperInvariant();
        var selectedType = await _dbContext.ProductTypes
            .AsNoTracking()
            .Where(item => item.Kind == ProductTypeKind.MainProduct
                && item.Code == normalizedTypeCode)
            .Select(item => new ProductTypeReference(item.Id, item.Code, item.Name))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (selectedType is null)
        {
            return Failure<byte[]>("Выбранный тип основного товара не найден.");
        }

        var needs = await _dbContext.ComponentNeedDefinitions
            .AsNoTracking()
            .Where(item => item.MainProductTypeId == selectedType.Id)
            .ToDictionaryAsync(item => item.Id, cancellationToken)
            .ConfigureAwait(false);
        var selectedManufacturerIds = (componentManufacturerIds ?? [])
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();
        var offersQuery = _dbContext.ComponentOffers
            .AsNoTracking()
            .Include(item => item.ConditionGroups)
            .ThenInclude(group => group.Constraints)
            .Where(item => item.MainProductTypeId == selectedType.Id);

        if (selectedManufacturerIds.Length > 0)
        {
            var matchingComponentIds = _dbContext.Products
                .AsNoTracking()
                .Where(product => selectedManufacturerIds.Contains(
                    product.ManufacturerId))
                .Select(product => product.Id);

            offersQuery = offersQuery.Where(offer =>
                matchingComponentIds.Contains(offer.ComponentProductId));
        }

        var offers = await offersQuery
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (selectedManufacturerIds.Length > 0 && offers.Count == 0)
        {
            return Failure<byte[]>(
                "Для выбранных производителей правила этого типа не найдены.");
        }
        var componentIds = offers
            .Select(item => item.ComponentProductId)
            .Distinct()
            .ToArray();
        var components = await _dbContext.Products
            .AsNoTracking()
            .Where(item => componentIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken)
            .ConfigureAwait(false);
        var manufacturerIdsUsedByComponents = components.Values
            .Select(item => item.ManufacturerId)
            .Distinct()
            .ToArray();
        var componentManufacturers = await _dbContext.Manufacturers
            .AsNoTracking()
            .Where(item => manufacturerIdsUsedByComponents.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken)
            .ConfigureAwait(false);
        var characteristics = await (
                from relation in _dbContext.ProductTypeCharacteristics.AsNoTracking()
                join definition in _dbContext.CharacteristicDefinitions.AsNoTracking()
                    on relation.CharacteristicDefinitionId equals definition.Id
                where relation.ProductTypeId == selectedType.Id
                orderby definition.Name
                select new CharacteristicExportReference(
                    definition.Id,
                    definition.Code,
                    definition.Name,
                    definition.DataType))
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Правила совместимости");
        var headers = new List<string>
        {
            "Артикул комплектующего",
            "Наименование комплектующего",
            "Производитель комплектующего",
            "Тип основного товара",
            "Потребность",
            "Активно"
        };
        var rangeDefinitionIds = offers
            .SelectMany(item => item.ConditionGroups)
            .SelectMany(item => item.Constraints)
            .Where(item => item.ComparisonOperator !=
                ComponentCompatibilityComparisonOperator.Equal)
            .Select(item => item.CharacteristicDefinitionId)
            .ToHashSet();
        var characteristicColumns = CreateCharacteristicColumns(
            characteristics,
            rangeDefinitionIds);
        headers.AddRange(characteristicColumns.Select(item => item.Header));

        for (var column = 0; column < headers.Count; column++)
        {
            worksheet.Cell(1, column + 1).Value = headers[column];
        }

        var rowNumber = 2;
        foreach (var offer in offers
                     .Where(item => components.ContainsKey(item.ComponentProductId))
                     .OrderBy(
                         item => item.NeedDefinitionId.HasValue
                             ? needs[item.NeedDefinitionId.Value].Name
                             : "Совместимые аксессуары",
                         StringComparer.OrdinalIgnoreCase)
                     .ThenBy(
                         item => components[item.ComponentProductId].Article.Value,
                         StringComparer.OrdinalIgnoreCase))
        {
            var groups = offer.ConditionGroups.OrderBy(item => item.Position).ToArray();
            if (groups.Length == 0)
            {
                groups = [null!];
            }

            foreach (var group in groups)
            {
                worksheet.Cell(rowNumber, 1).Value =
                    components[offer.ComponentProductId].Article.Value;
                worksheet.Cell(rowNumber, 2).Value =
                    components[offer.ComponentProductId].Name.Value;
                worksheet.Cell(rowNumber, 3).Value = componentManufacturers.GetValueOrDefault(
                    components[offer.ComponentProductId].ManufacturerId,
                    string.Empty);
                worksheet.Cell(rowNumber, 4).Value = selectedType.Code;
                worksheet.Cell(rowNumber, 5).Value = offer.NeedDefinitionId.HasValue
                    ? needs[offer.NeedDefinitionId.Value].Name
                    : string.Empty;
                worksheet.Cell(rowNumber, 6).Value = offer.IsActive ? "Да" : "Нет";

                if (group is not null)
                {
                    var constraintsByDefinition = group.Constraints
                        .GroupBy(item => item.CharacteristicDefinitionId)
                        .ToDictionary(item => item.Key, item => item.ToArray());

                    for (var index = 0; index < characteristicColumns.Count; index++)
                    {
                        var column = characteristicColumns[index];
                        if (!constraintsByDefinition.TryGetValue(
                                column.Characteristic.Id,
                                out var constraints))
                        {
                            continue;
                        }

                        var matchingConstraints = constraints
                            .Where(item => item.ComparisonOperator ==
                                column.ComparisonOperator)
                            .ToArray();
                        if (matchingConstraints.Length == 0)
                        {
                            continue;
                        }

                        worksheet.Cell(rowNumber, index + 7).Value = string.Join(
                            ";",
                            matchingConstraints.Select(item =>
                                ComponentCompatibilityCharacteristicValueConverter
                                    .FormatForExcel(
                                        column.Characteristic.Code,
                                        item.ExpectedValue)));
                    }
                }

                rowNumber++;
            }
        }

        StyleRulesWorksheet(worksheet, headers.Count);
        AddMetadataWorksheet(workbook, selectedType, characteristicColumns);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<Result<ComponentCompatibilityImportPreview, DomainError>>
        PreviewAsync(
            Stream workbookStream,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workbookStream);

        try
        {
            using var workbook = new XLWorkbook(workbookStream);
            var worksheet = FindRulesWorksheet(workbook);

            if (worksheet is null)
            {
                return Failure<ComponentCompatibilityImportPreview>(
                    "В файле не найден лист «Совместимость». Добавьте его или скачайте объединённый шаблон.");
            }

            var headers = ReadHeaders(worksheet);
            var metadataByHeader = ReadMetadataCharacteristics(workbook);
            var articleColumn = FindColumn(
                headers,
                "АРТИКУЛ КОМПЛЕКТУЮЩЕГО",
                "АРТИКУЛ");
            var manufacturerColumn = FindColumn(
                headers,
                "ПРОИЗВОДИТЕЛЬ КОМПЛЕКТУЮЩЕГО",
                "ПРОИЗВОДИТЕЛЬ",
                "БРЕНД");
            var mainTypeColumn = FindColumn(headers, "ТИП ОСНОВНОГО ТОВАРА");
            var needColumn = FindColumn(headers, "ПОТРЕБНОСТЬ");
            var activeColumn = FindColumn(headers, "АКТИВНО", "СОСТОЯНИЕ");

            if (!articleColumn.HasValue
                || !mainTypeColumn.HasValue
                || !needColumn.HasValue)
            {
                return Failure<ComponentCompatibilityImportPreview>(
                    "Нужны колонки «Артикул комплектующего» (или «Артикул»), "
                    + "«Тип основного товара» и «Потребность».");
            }

            var referenceData = await LoadReferenceDataAsync(cancellationToken)
                .ConfigureAwait(false);
            var rows = new List<ComponentCompatibilityImportPreviewRow>();
            var representedGroups = new HashSet<string>(StringComparer.Ordinal);
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

            if (lastRow - 1 > MaximumRowsCount)
            {
                return Failure<ComponentCompatibilityImportPreview>(
                    $"Файл содержит больше {MaximumRowsCount} строк.");
            }

            for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var row = worksheet.Row(rowNumber);
                var article = ReadCell(row, articleColumn.Value);
                var manufacturer = manufacturerColumn.HasValue
                    ? ReadCell(row, manufacturerColumn.Value)
                    : string.Empty;
                var mainTypeValue = ReadCell(row, mainTypeColumn.Value);
                var needValue = ReadCell(row, needColumn.Value);
                var activeResult = ParseActiveValue(
                    activeColumn.HasValue
                        ? ReadCell(row, activeColumn.Value)
                        : string.Empty);

                if (article.Length == 0
                    && mainTypeValue.Length == 0
                    && needValue.Length == 0)
                {
                    continue;
                }

                if (activeResult.IsFailure)
                {
                    rows.Add(ProblemRow(
                        rowNumber,
                        article,
                        mainTypeValue,
                        needValue,
                        ComponentCompatibilityImportRowStatus.Invalid,
                        activeResult.Error.Message));
                    continue;
                }

                var componentCandidates = referenceData.ComponentProducts
                    .Where(item => string.Equals(
                        Normalize(item.Article),
                        Normalize(article),
                        StringComparison.Ordinal)
                        && (manufacturer.Length == 0
                            || string.Equals(
                                Normalize(item.ManufacturerName),
                                Normalize(manufacturer),
                                StringComparison.Ordinal)))
                    .ToArray();

                if (componentCandidates.Length != 1)
                {
                    rows.Add(ProblemRow(
                        rowNumber,
                        article,
                        mainTypeValue,
                        needValue,
                        ComponentCompatibilityImportRowStatus.ComponentNotFound,
                        componentCandidates.Length == 0
                            ? "Комплектующее с таким артикулом не найдено."
                            : "По артикулу найдено несколько комплектующих."));
                    continue;
                }

                var mainTypeCandidates = referenceData.MainProductTypes
                    .Where(item =>
                        string.Equals(
                            Normalize(item.Code),
                            Normalize(mainTypeValue),
                            StringComparison.Ordinal)
                        || string.Equals(
                            Normalize(item.Name),
                            Normalize(mainTypeValue),
                            StringComparison.Ordinal))
                    .ToArray();

                if (mainTypeCandidates.Length != 1)
                {
                    rows.Add(ProblemRow(
                        rowNumber,
                        article,
                        mainTypeValue,
                        needValue,
                        ComponentCompatibilityImportRowStatus
                            .MainProductTypeNotFound,
                        "Тип основного товара не найден однозначно."));
                    continue;
                }

                var mainType = mainTypeCandidates[0];
                NeedReference? need = null;
                if (needValue.Length > 0)
                {
                    var needCandidates = referenceData.Needs
                        .Where(item =>
                            item.MainProductTypeId == mainType.Id
                            && (string.Equals(
                                    Normalize(item.Code),
                                    Normalize(needValue),
                                    StringComparison.Ordinal)
                                || string.Equals(
                                    Normalize(item.Name),
                                    Normalize(needValue),
                                    StringComparison.Ordinal)))
                        .ToArray();

                    if (needCandidates.Length != 1)
                    {
                        rows.Add(ProblemRow(
                            rowNumber,
                            article,
                            mainTypeValue,
                            needValue,
                            ComponentCompatibilityImportRowStatus.NeedNotFound,
                            "Потребность не найдена для выбранного типа товара."));
                        continue;
                    }

                    need = needCandidates[0];
                }

                var component = componentCandidates[0];
                var constraintResult = CreateConstraints(
                    row,
                    headers,
                    mainType.Id,
                    referenceData.Characteristics,
                    referenceData.Definitions,
                    articleColumn.Value,
                    mainTypeColumn.Value,
                    needColumn.Value,
                    activeColumn,
                    metadataByHeader);

                if (constraintResult.IsFailure)
                {
                    rows.Add(ProblemRow(
                        rowNumber,
                        article,
                        mainTypeValue,
                        needValue,
                        ComponentCompatibilityImportRowStatus
                            .CharacteristicNotFound,
                        constraintResult.Error.Message));
                    continue;
                }

                var groupSignature = CreateGroupSignature(
                    component.Id,
                    mainType.Id,
                    need?.Id,
                    constraintResult.Value);

                if (!representedGroups.Add(groupSignature))
                {
                    rows.Add(ProblemRow(
                        rowNumber,
                        article,
                        mainTypeValue,
                        needValue,
                        ComponentCompatibilityImportRowStatus.Duplicate,
                        "Полностью одинаковый вариант уже встречался выше в файле."));
                    continue;
                }

                rows.Add(new ComponentCompatibilityImportPreviewRow(
                    rowNumber,
                    article,
                    mainType.Name,
                    need?.Name ?? string.Empty,
                    component.Id,
                    mainType.Id,
                    need?.Id,
                    activeResult.Value,
                    ComponentCompatibilityImportRowStatus.Ready,
                    null,
                    constraintResult.Value));
            }

            rows = ClassifyPreviewRows(rows, referenceData.ExistingRules);

            return new ComponentCompatibilityImportPreview(
                rows.Count,
                rows.Count(item =>
                    item.Status is ComponentCompatibilityImportRowStatus.WillCreate
                        or ComponentCompatibilityImportRowStatus.WillUpdate),
                rows.Count(item =>
                    item.Status is not (
                        ComponentCompatibilityImportRowStatus.WillCreate
                        or ComponentCompatibilityImportRowStatus.WillUpdate
                        or ComponentCompatibilityImportRowStatus.Unchanged)),
                rows);
        }
        catch (Exception exception) when (
            exception is not OperationCanceledException)
        {
            return Failure<ComponentCompatibilityImportPreview>(
                "Не удалось прочитать Excel-файл правил совместимости.");
        }
    }

    private static IXLWorksheet? FindRulesWorksheet(XLWorkbook workbook)
    {
        var preferredNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "СОВМЕСТИМОСТЬ",
            "ПРАВИЛА СОВМЕСТИМОСТИ"
        };

        var rulesWorksheet = workbook.Worksheets
            .FirstOrDefault(sheet =>
                preferredNames.Contains(Normalize(sheet.Name))
                && sheet.LastCellUsed() is not null);

        if (rulesWorksheet is not null)
        {
            return rulesWorksheet;
        }

        var isUnifiedWorkbook = workbook.Worksheets.Any(sheet =>
            string.Equals(
                Normalize(sheet.Name),
                "ТОВАРЫ",
                StringComparison.Ordinal));

        return isUnifiedWorkbook
            ? null
            : workbook.Worksheets.FirstOrDefault(sheet =>
                sheet.LastCellUsed() is not null);
    }

    private static void AddProductsWorksheet(XLWorkbook workbook)
    {
        var worksheet = workbook.Worksheets.Add("Товары");
        worksheet.Position = 1;
        string[] headers =
        [
            "Производитель",
            "Артикул",
            "Наименование",
            "Назначение товара",
            "Тип товара",
            "Цена",
            "Остаток"
        ];

        for (var index = 0; index < headers.Length; index++)
        {
            worksheet.Cell(1, index + 1).Value = headers[index];
        }

        worksheet.Cell(2, 1).Value = "Производитель";
        worksheet.Cell(2, 2).Value = "АРТИКУЛ-001";
        worksheet.Cell(2, 3).Value = "Наименование товара";
        worksheet.Cell(2, 4).Value = "Комплектующее";
        worksheet.Cell(2, 5).Value = "Комплектующие";
        worksheet.Cell(2, 6).Value = 0;
        worksheet.Cell(2, 7).Value = 0;

        var headerRange = worksheet.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#0F766E");
        headerRange.Style.Font.FontColor = XLColor.White;
        worksheet.SheetView.FreezeRows(1);
        worksheet.Column(1).Width = 24;
        worksheet.Column(2).Width = 24;
        worksheet.Column(3).Width = 52;
        worksheet.Column(4).Width = 22;
        worksheet.Column(5).Width = 28;
        worksheet.Column(6).Width = 16;
        worksheet.Column(7).Width = 16;
        worksheet.Range(2, 4, 5000, 4).CreateDataValidation()
            .List("\"Основной товар,Комплектующее\"", true);
        worksheet.RangeUsed()?.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Top);
    }

    public async Task<Result<int, DomainError>> ApplyAsync(
        IReadOnlyList<ApplyComponentCompatibilityImportRow> rows,
        bool allowRulesWithoutConditions = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rows);

        if (rows.Count is 0 or > MaximumRowsCount)
        {
            return Failure<int>("Нет правил для применения или превышен лимит строк.");
        }

        if (rows.Any(row =>
                row.ComponentProductId == Guid.Empty
                || row.MainProductTypeId == Guid.Empty
                || row.NeedDefinitionId == Guid.Empty))
        {
            return Failure<int>("В запросе есть некорректные правила.");
        }

        var groupedRows = rows
            .GroupBy(row => (
                row.ComponentProductId,
                row.MainProductTypeId,
                row.NeedDefinitionId))
            .ToArray();
        var requestedKeys = groupedRows.Select(group => group.Key).ToHashSet();

        if (!allowRulesWithoutConditions
            && groupedRows.Any(group => group.Any(row => row.Constraints.Count == 0)))
        {
            return Failure<int>(
                "В файле есть правило без условий. Оно подойдёт всем товарам выбранного типа. Подтвердите создание широкого правила отдельно.");
        }

        if (groupedRows.Any(group =>
                group.Count() > 1
                && group.Any(row => row.Constraints.Count == 0)))
        {
            return Failure<int>(
                "Вариант без условий уже подходит всем товарам типа и не может "
                + "использоваться вместе с другими вариантами одного правила.");
        }


        if (groupedRows.Any(group =>
                group.Select(row => row.IsActive).Distinct().Count() > 1))
        {
            return Failure<int>(
                "Все строки одного правила должны иметь одинаковое значение «Активно». ");
        }

        var productIds = rows.Select(row => row.ComponentProductId).Distinct().ToArray();
        var needIds = rows
            .Where(row => row.NeedDefinitionId.HasValue)
            .Select(row => row.NeedDefinitionId!.Value)
            .Distinct()
            .ToArray();
        var mainProductTypeIds = rows
            .Select(row => row.MainProductTypeId)
            .Distinct()
            .ToArray();
        var definitionIds = rows
            .SelectMany(row => row.Constraints)
            .Select(item => item.CharacteristicDefinitionId)
            .Distinct()
            .ToArray();
        var componentIds = await (
                from product in _dbContext.Products.AsNoTracking()
                join productType in _dbContext.ProductTypes.AsNoTracking()
                    on product.ProductTypeId equals productType.Id
                where productIds.Contains(product.Id)
                      && productType.Kind == ProductTypeKind.Component
                select product.Id)
            .ToHashSetAsync(cancellationToken)
            .ConfigureAwait(false);
        var needs = await _dbContext.ComponentNeedDefinitions
            .Where(item => needIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken)
            .ConfigureAwait(false);
        var mainProductTypes = await _dbContext.ProductTypes
            .Where(item => mainProductTypeIds.Contains(item.Id)
                && item.Kind == ProductTypeKind.MainProduct)
            .ToDictionaryAsync(item => item.Id, cancellationToken)
            .ConfigureAwait(false);
        var definitions = await _dbContext.CharacteristicDefinitions
            .Where(item => definitionIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken)
            .ConfigureAwait(false);
        var allowedPairs = await _dbContext.ProductTypeCharacteristics
            .Where(item => mainProductTypeIds.Contains(item.ProductTypeId))
            .Select(item => new
            {
                item.ProductTypeId,
                item.CharacteristicDefinitionId
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var allowed = allowedPairs
            .Select(item => (item.ProductTypeId, item.CharacteristicDefinitionId))
            .ToHashSet();
        var existingOffers = await _dbContext.ComponentOffers
            .Include(item => item.ConditionGroups)
            .ThenInclude(group => group.Constraints)
            .Where(item => productIds.Contains(item.ComponentProductId)
                && mainProductTypeIds.Contains(item.MainProductTypeId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var targetExistingOffers = existingOffers
            .Where(item => requestedKeys.Contains(
                (item.ComponentProductId,
                    item.MainProductTypeId,
                    item.NeedDefinitionId)))
            .ToList();
        var existingByKey = targetExistingOffers.ToDictionary(
            item => (
                item.ComponentProductId,
                item.MainProductTypeId,
                item.NeedDefinitionId));
        var preparedRules = new List<PreparedImportRule>();

        foreach (var ruleRows in groupedRows)
        {
            if (!componentIds.Contains(ruleRows.Key.ComponentProductId)
                || !mainProductTypes.ContainsKey(ruleRows.Key.MainProductTypeId)
                || (ruleRows.Key.NeedDefinitionId.HasValue
                    && (!needs.TryGetValue(
                            ruleRows.Key.NeedDefinitionId.Value,
                            out var selectedNeed)
                        || selectedNeed.MainProductTypeId !=
                            ruleRows.Key.MainProductTypeId)))
            {
                return Failure<int>(
                    "Состав каталога изменился. Выполните предпросмотр повторно.");
            }

            var preparedGroups = new List<IReadOnlyCollection<(
                Guid DefinitionId,
                CharacteristicValue Value,
                ComponentCompatibilityComparisonOperator ComparisonOperator)>>();

            foreach (var rowConstraints in ruleRows.Select(row => row.Constraints))
            {
                var preparedConstraints = new List<(
                    Guid DefinitionId,
                    CharacteristicValue Value,
                    ComponentCompatibilityComparisonOperator ComparisonOperator)>();

                foreach (var constraint in rowConstraints)
                {
                    if (!definitions.TryGetValue(
                            constraint.CharacteristicDefinitionId,
                            out var definition)
                        || !allowed.Contains(
                            (ruleRows.Key.MainProductTypeId, definition.Id)))
                    {
                        return Failure<int>(
                            "Характеристика больше не разрешена для типа товара.");
                    }

                    var valueResult =
                        ComponentCompatibilityCharacteristicValueConverter.Parse(
                            definition,
                            constraint.Value);

                    if (valueResult.IsFailure)
                    {
                        return Result.Failure<int, DomainError>(valueResult.Error);
                    }

                    if (constraint.ComparisonOperator !=
                            ComponentCompatibilityComparisonOperator.Equal
                        && definition.DataType != CharacteristicDataType.Number)
                    {
                        return Failure<int>(
                            "Границы «от/до» доступны только для числовых характеристик.");
                    }

                    preparedConstraints.Add((
                        definition.Id,
                        valueResult.Value,
                        constraint.ComparisonOperator));
                }

                var rangeValidation = ValidateNumericRanges(rowConstraints);
                if (rangeValidation.IsFailure)
                {
                    return Result.Failure<int, DomainError>(rangeValidation.Error);
                }

                preparedGroups.Add(preparedConstraints);
            }

            preparedRules.Add(new PreparedImportRule(
                ruleRows.Key.ComponentProductId,
                ruleRows.Key.MainProductTypeId,
                ruleRows.Key.NeedDefinitionId,
                ruleRows.First().IsActive,
                preparedGroups));
        }

        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        _dbContext.ComponentCompatibilityConditionGroups.RemoveRange(
            targetExistingOffers.SelectMany(offer => offer.ConditionGroups));
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        foreach (var preparedRule in preparedRules)
        {
            ComponentOffer offer;
            var key = (
                preparedRule.ComponentProductId,
                preparedRule.MainProductTypeId,
                preparedRule.NeedDefinitionId);

            if (existingByKey.TryGetValue(key, out var existingOffer))
            {
                offer = existingOffer;
                offer.ClearConditionGroups();
            }
            else
            {
                var offerResult = ComponentOffer.Create(
                    preparedRule.ComponentProductId,
                    preparedRule.MainProductTypeId,
                    preparedRule.NeedDefinitionId);

                if (offerResult.IsFailure)
                {
                    return Result.Failure<int, DomainError>(offerResult.Error);
                }

                offer = offerResult.Value;
                offer.ClearConditionGroups();
                _dbContext.ComponentOffers.Add(offer);
            }

            foreach (var preparedGroup in preparedRule.Groups)
            {
                var groupResult = offer.AddConditionGroup();

                if (groupResult.IsFailure)
                {
                    return Result.Failure<int, DomainError>(groupResult.Error);
                }

                foreach (var constraint in preparedGroup)
                {
                    var addResult = groupResult.Value.AddConstraint(
                        constraint.DefinitionId,
                        constraint.Value,
                        constraint.ComparisonOperator);

                    if (addResult.IsFailure)
                    {
                        return Result.Failure<int, DomainError>(addResult.Error);
                    }
                }
            }

            offer.SetActive(preparedRule.IsActive);
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return preparedRules.Count;
    }

    private async Task<ReferenceData> LoadReferenceDataAsync(
        CancellationToken cancellationToken)
    {
        var componentProducts = await (
                from product in _dbContext.Products.AsNoTracking()
                join productType in _dbContext.ProductTypes.AsNoTracking()
                    on product.ProductTypeId equals productType.Id
                join manufacturer in _dbContext.Manufacturers.AsNoTracking()
                    on product.ManufacturerId equals manufacturer.Id
                where productType.Kind == ProductTypeKind.Component
                select new ProductReference(
                    product.Id,
                    product.Article.Value,
                    manufacturer.Name))
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        var mainProductTypes = await _dbContext.ProductTypes
            .AsNoTracking()
            .Where(item => item.Kind == ProductTypeKind.MainProduct)
            .Select(item => new ProductTypeReference(item.Id, item.Code, item.Name))
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        var needs = await _dbContext.ComponentNeedDefinitions
            .AsNoTracking()
            .Select(item => new NeedReference(
                item.Id,
                item.MainProductTypeId,
                item.Code,
                item.Name))
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        var characteristics = await (
                from relation in _dbContext.ProductTypeCharacteristics.AsNoTracking()
                join definition in _dbContext.CharacteristicDefinitions.AsNoTracking()
                    on relation.CharacteristicDefinitionId equals definition.Id
                select new CharacteristicReference(
                    relation.ProductTypeId,
                    definition.Id,
                    definition.Code,
                    definition.Name,
                    definition.DataType))
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        var definitions = await _dbContext.CharacteristicDefinitions
            .AsNoTracking()
            .ToDictionaryAsync(item => item.Id, cancellationToken)
            .ConfigureAwait(false);
        var existingOffers = await _dbContext.ComponentOffers
            .AsNoTracking()
            .Include(item => item.ConditionGroups)
            .ThenInclude(group => group.Constraints)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new ReferenceData(
            componentProducts,
            mainProductTypes,
            needs,
            characteristics,
            definitions,
            existingOffers.ToDictionary(
                item => (
                    item.ComponentProductId,
                    item.MainProductTypeId,
                    item.NeedDefinitionId),
                item => new ExistingRuleReference(
                    item.IsActive,
                    item.ConditionGroups
                        .Select(group => CreateGroupSignature(
                            item.ComponentProductId,
                            item.MainProductTypeId,
                            item.NeedDefinitionId,
                            group.Constraints.Select(constraint =>
                                new ComponentCompatibilityImportConstraint(
                                    constraint.CharacteristicDefinitionId,
                                    string.Empty,
                                    ComponentCompatibilityCharacteristicValueConverter
                                        .FormatCanonical(constraint.ExpectedValue),
                                    constraint.ComparisonOperator))
                                .ToArray()))
                        .ToHashSet(StringComparer.Ordinal))));
    }

    private static Result<
        IReadOnlyList<ComponentCompatibilityImportConstraint>,
        DomainError> CreateConstraints(
            IXLRow row,
            IReadOnlyDictionary<int, string> headers,
            Guid mainProductTypeId,
            IReadOnlyCollection<CharacteristicReference> characteristics,
            IReadOnlyDictionary<Guid, CharacteristicDefinition> definitions,
            int articleColumn,
            int mainTypeColumn,
            int needColumn,
            int? activeColumn,
            IReadOnlyDictionary<string, CharacteristicColumnMetadata> metadataByHeader)
    {
        var result = new List<ComponentCompatibilityImportConstraint>();

        foreach (var header in headers)
        {
            if (header.Key == articleColumn
                || header.Key == mainTypeColumn
                || header.Key == needColumn
                || header.Key == activeColumn
                || IsIgnoredStandardHeader(header.Value))
            {
                continue;
            }

            var rawValue = ReadCell(row, header.Key);

            if (rawValue.Length == 0)
            {
                continue;
            }

            var normalizedHeader = Normalize(header.Value);
            metadataByHeader.TryGetValue(normalizedHeader, out var columnMetadata);
            var comparisonOperator = columnMetadata?.ComparisonOperator
                ?? GetComparisonOperatorFromHeader(header.Value);
            var characteristicHeader = columnMetadata?.CharacteristicCode
                ?? StripRangeSuffix(header.Value, comparisonOperator);
            var candidates = characteristics
                .Where(item =>
                    item.ProductTypeId == mainProductTypeId
                    && (string.Equals(
                            Normalize(item.Code),
                            Normalize(characteristicHeader),
                            StringComparison.Ordinal)
                        || string.Equals(
                            Normalize(item.Name),
                            Normalize(characteristicHeader),
                            StringComparison.Ordinal)))
                .ToArray();

            if (candidates.Length != 1)
            {
                return Failure<IReadOnlyList<ComponentCompatibilityImportConstraint>>(
                    $"Колонка «{header.Value}» не соответствует характеристике "
                    + "выбранного типа основного товара.");
            }

            var rawValues = rawValue
                         .Split(';', StringSplitOptions.TrimEntries
                             | StringSplitOptions.RemoveEmptyEntries);

            if (comparisonOperator != ComponentCompatibilityComparisonOperator.Equal
                && rawValues.Length != 1)
            {
                return Failure<IReadOnlyList<ComponentCompatibilityImportConstraint>>(
                    $"В колонке диапазона «{header.Value}» укажите одно число.");
            }

            foreach (var value in rawValues)
            {
                if (!definitions.TryGetValue(
                        candidates[0].DefinitionId,
                        out var definition))
                {
                    return Failure<IReadOnlyList<ComponentCompatibilityImportConstraint>>(
                        $"Характеристика «{candidates[0].Name}» не найдена.");
                }

                if (comparisonOperator !=
                        ComponentCompatibilityComparisonOperator.Equal
                    && definition.DataType != CharacteristicDataType.Number)
                {
                    return Failure<IReadOnlyList<ComponentCompatibilityImportConstraint>>(
                        $"Границы «от/до» доступны только для числовых характеристик. Колонка: «{header.Value}».");
                }

                var valueResult =
                    ComponentCompatibilityCharacteristicValueConverter.Parse(
                        definition,
                        value);

                if (valueResult.IsFailure)
                {
                    return Failure<IReadOnlyList<ComponentCompatibilityImportConstraint>>(
                        valueResult.Error.Message);
                }

                result.Add(new ComponentCompatibilityImportConstraint(
                    candidates[0].DefinitionId,
                    candidates[0].Name,
                    ComponentCompatibilityCharacteristicValueConverter
                        .FormatCanonical(valueResult.Value),
                    comparisonOperator));
            }
        }

        var rangeValidation = ValidateNumericRanges(result);
        if (rangeValidation.IsFailure)
        {
            return Failure<IReadOnlyList<ComponentCompatibilityImportConstraint>>(
                rangeValidation.Error.Message);
        }

        return result;
    }

    private static Result<bool, DomainError> ParseActiveValue(string rawValue)
    {
        var normalized = Normalize(rawValue);
        if (normalized.Length == 0 || normalized is "ДА" or "TRUE" or "1" or "АКТИВНО")
        {
            return true;
        }

        if (normalized is "НЕТ" or "FALSE" or "0" or "ОТКЛЮЧЕНО")
        {
            return false;
        }

        return Failure<bool>(
            "В колонке «Активно» допустимы значения «Да» или «Нет».");
    }

    private static Dictionary<int, string> ReadHeaders(
        IXLWorksheet worksheet)
    {
        var result = new Dictionary<int, string>();
        var lastColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;

        for (var column = 1; column <= lastColumn; column++)
        {
            var header = worksheet.Cell(1, column).GetFormattedString().Trim();

            if (header.Length > 0)
            {
                result[column] = header;
            }
        }

        return result;
    }

    private static Dictionary<string, CharacteristicColumnMetadata>
        ReadMetadataCharacteristics(XLWorkbook workbook)
    {
        if (!workbook.TryGetWorksheet("_Метаданные", out var metadata))
        {
            return new Dictionary<string, CharacteristicColumnMetadata>(
                StringComparer.Ordinal);
        }

        var result = new Dictionary<string, CharacteristicColumnMetadata>(
            StringComparer.Ordinal);
        var lastRow = metadata.LastRowUsed()?.RowNumber() ?? 3;
        for (var row = 4; row <= lastRow; row++)
        {
            var code = metadata.Cell(row, 1).GetFormattedString().Trim();
            var name = metadata.Cell(row, 2).GetFormattedString().Trim();
            var operatorText = metadata.Cell(row, 4).GetFormattedString().Trim();
            if (code.Length > 0 && name.Length > 0)
            {
                var comparisonOperator = Enum.TryParse<
                        ComponentCompatibilityComparisonOperator>(
                        operatorText,
                        true,
                        out var parsedOperator)
                    ? parsedOperator
                    : GetComparisonOperatorFromHeader(name);
                result[Normalize(name)] = new CharacteristicColumnMetadata(
                    code,
                    comparisonOperator);
            }
        }

        return result;
    }

    private static int? FindColumn(
        IReadOnlyDictionary<int, string> headers,
        params string[] names)
    {
        var normalizedNames = names.Select(Normalize).ToHashSet(StringComparer.Ordinal);
        var match = headers.FirstOrDefault(item =>
            normalizedNames.Contains(Normalize(item.Value)));

        return match.Key == 0 ? null : match.Key;
    }

    private static bool IsIgnoredStandardHeader(string header)
    {
        return Normalize(header) is "НАИМЕНОВАНИЕ КОМПЛЕКТУЮЩЕГО"
            or "НАИМЕНОВАНИЕ"
            or "ПРОИЗВОДИТЕЛЬ КОМПЛЕКТУЮЩЕГО"
            or "ПРОИЗВОДИТЕЛЬ"
            or "БРЕНД"
            or "ТИП ТОВАРА";
    }

    private static string ReadCell(IXLRow row, int column)
    {
        return row.Cell(column).GetFormattedString().Trim();
    }

    private static string Normalize(string value)
    {
        return string.Join(
            ' ',
            value.Trim().ToUpperInvariant()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string CreateGroupSignature(
        Guid componentProductId,
        Guid mainProductTypeId,
        Guid? needDefinitionId,
        IReadOnlyCollection<ComponentCompatibilityImportConstraint> constraints)
    {
        var values = constraints
            .OrderBy(item => item.CharacteristicDefinitionId)
            .ThenBy(item => item.ComparisonOperator)
            .ThenBy(item => item.Value, StringComparer.OrdinalIgnoreCase)
            .Select(item =>
                $"{item.CharacteristicDefinitionId:N}:"
                + $"{item.ComparisonOperator}:"
                + Normalize(item.Value));

        return $"{componentProductId:N}|{mainProductTypeId:N}|"
            + $"{needDefinitionId?.ToString("N") ?? "DIRECT"}|"
            + string.Join('|', values);
    }

    private static ComponentCompatibilityImportPreviewRow ProblemRow(
        int rowNumber,
        string article,
        string mainType,
        string need,
        ComponentCompatibilityImportRowStatus status,
        string message)
    {
        return new ComponentCompatibilityImportPreviewRow(
            rowNumber,
            article,
            mainType,
            need,
            null,
            null,
            null,
            true,
            status,
            message,
            []);
    }

    private static List<ComponentCompatibilityImportPreviewRow> ClassifyPreviewRows(
        IReadOnlyCollection<ComponentCompatibilityImportPreviewRow> rows,
        IReadOnlyDictionary<
            (Guid ProductId, Guid MainProductTypeId, Guid? NeedId),
            ExistingRuleReference>
            existingRules)
    {
        var result = rows.ToList();
        var validGroups = rows
            .Select((row, index) => new { Row = row, Index = index })
            .Where(item => item.Row.Status == ComponentCompatibilityImportRowStatus.Ready
                && item.Row.ComponentProductId.HasValue
                && item.Row.MainProductTypeId.HasValue)
            .GroupBy(item => (
                ProductId: item.Row.ComponentProductId!.Value,
                MainProductTypeId: item.Row.MainProductTypeId!.Value,
                NeedId: item.Row.NeedDefinitionId));

        foreach (var ruleRows in validGroups)
        {
            if (ruleRows.Select(item => item.Row.IsActive).Distinct().Count() > 1)
            {
                foreach (var item in ruleRows)
                {
                    result[item.Index] = item.Row with
                    {
                        Status = ComponentCompatibilityImportRowStatus.Invalid,
                        Message = "Все варианты одного правила должны иметь одинаковое значение «Активно»."
                    };
                }

                continue;
            }

            var groupSignatures = ruleRows
                .Select(item => CreateGroupSignature(
                    ruleRows.Key.ProductId,
                    ruleRows.Key.MainProductTypeId,
                    ruleRows.Key.NeedId,
                    item.Row.Constraints))
                .ToHashSet(StringComparer.Ordinal);
            var isActive = ruleRows.First().Row.IsActive;
            ComponentCompatibilityImportRowStatus status;
            string message;

            if (!existingRules.TryGetValue(ruleRows.Key, out var existingRule))
            {
                status = ComponentCompatibilityImportRowStatus.WillCreate;
                message = "Будет создано новое правило.";
            }
            else if (existingRule.IsActive == isActive
                     && existingRule.GroupSignatures.SetEquals(groupSignatures))
            {
                status = ComponentCompatibilityImportRowStatus.Unchanged;
                message = "Условия и состояние уже совпадают с сохранённым правилом.";
            }
            else
            {
                status = ComponentCompatibilityImportRowStatus.WillUpdate;
                message = "Существующее правило будет обновлено вариантами из файла.";
            }

            if (status is ComponentCompatibilityImportRowStatus.WillCreate
                    or ComponentCompatibilityImportRowStatus.WillUpdate
                && ruleRows.Any(item => item.Row.Constraints.Count == 0))
            {
                status = ComponentCompatibilityImportRowStatus.RequiresConfirmation;
                message =
                    "Правило не содержит условий и будет подходить всем товарам выбранного типа. Требуется отдельное подтверждение.";
            }

            foreach (var item in ruleRows)
            {
                result[item.Index] = item.Row with { Status = status, Message = message };
            }
        }

        return result;
    }

    private static void StyleRulesWorksheet(
        IXLWorksheet worksheet,
        int columnsCount)
    {
        worksheet.Range(1, 1, 1, columnsCount).Style.Font.Bold = true;
        worksheet.Range(1, 1, 1, columnsCount).Style.Fill.BackgroundColor =
            XLColor.FromHtml("#DDEBF7");
        worksheet.SheetView.FreezeRows(1);
        worksheet.Columns().AdjustToContents();
        worksheet.Column(1).Width = Math.Max(worksheet.Column(1).Width, 28);
        worksheet.Column(2).Width = Math.Max(worksheet.Column(2).Width, 42);
        worksheet.Column(3).Width = Math.Max(worksheet.Column(3).Width, 28);
        worksheet.Column(4).Width = Math.Max(worksheet.Column(4).Width, 28);
        worksheet.Column(5).Width = Math.Max(worksheet.Column(5).Width, 28);
        worksheet.Column(6).Width = Math.Max(worksheet.Column(6).Width, 14);
        worksheet.Range(2, 6, Math.Max(2, worksheet.LastRowUsed()?.RowNumber() ?? 2), 6)
            .CreateDataValidation()
            .List("\"Да,Нет\"", true);
    }

    private static void AddMetadataWorksheet(
        XLWorkbook workbook,
        ProductTypeReference selectedType,
        IReadOnlyCollection<CharacteristicColumnReference> columns)
    {
        var metadata = workbook.Worksheets.Add("_Метаданные");
        metadata.Cell(1, 1).Value = "Тип основного товара";
        metadata.Cell(1, 2).Value = selectedType.Code;
        metadata.Cell(3, 1).Value = "Код характеристики";
        metadata.Cell(3, 2).Value = "Название колонки";
        metadata.Cell(3, 3).Value = "Тип значения";
        metadata.Cell(3, 4).Value = "Оператор";

        var row = 4;
        foreach (var column in columns)
        {
            metadata.Cell(row, 1).Value = column.Characteristic.Code;
            metadata.Cell(row, 2).Value = column.Header;
            metadata.Cell(row, 3).Value = column.Characteristic.DataType.ToString();
            metadata.Cell(row, 4).Value = column.ComparisonOperator.ToString();
            row++;
        }

        metadata.Visibility = XLWorksheetVisibility.VeryHidden;
    }

    private static List<CharacteristicColumnReference>
        CreateCharacteristicColumns(
            IReadOnlyCollection<CharacteristicExportReference> characteristics,
            HashSet<Guid> rangeDefinitionIds)
    {
        var result = new List<CharacteristicColumnReference>();
        foreach (var characteristic in characteristics)
        {
            result.Add(new CharacteristicColumnReference(
                characteristic,
                characteristic.Name,
                ComponentCompatibilityComparisonOperator.Equal));

            if (characteristic.DataType == CharacteristicDataType.Number
                && rangeDefinitionIds.Contains(characteristic.Id))
            {
                result.Add(new CharacteristicColumnReference(
                    characteristic,
                    $"{characteristic.Name} от",
                    ComponentCompatibilityComparisonOperator.GreaterThanOrEqual));
                result.Add(new CharacteristicColumnReference(
                    characteristic,
                    $"{characteristic.Name} до",
                    ComponentCompatibilityComparisonOperator.LessThanOrEqual));
            }
        }

        return result;
    }

    private static ComponentCompatibilityComparisonOperator
        GetComparisonOperatorFromHeader(string header)
    {
        var normalized = Normalize(header);
        if (normalized.EndsWith(" ОТ", StringComparison.Ordinal))
        {
            return ComponentCompatibilityComparisonOperator.GreaterThanOrEqual;
        }

        if (normalized.EndsWith(" ДО", StringComparison.Ordinal))
        {
            return ComponentCompatibilityComparisonOperator.LessThanOrEqual;
        }

        return ComponentCompatibilityComparisonOperator.Equal;
    }

    private static string StripRangeSuffix(
        string header,
        ComponentCompatibilityComparisonOperator comparisonOperator)
    {
        var suffix = comparisonOperator switch
        {
            ComponentCompatibilityComparisonOperator.GreaterThanOrEqual => " ОТ",
            ComponentCompatibilityComparisonOperator.LessThanOrEqual => " ДО",
            _ => string.Empty
        };

        var normalized = Normalize(header);
        return suffix.Length > 0
            && normalized.EndsWith(suffix, StringComparison.Ordinal)
                ? normalized[..^suffix.Length].TrimEnd()
                : header;
    }

    private static UnitResult<DomainError> ValidateNumericRanges(
        IReadOnlyCollection<ComponentCompatibilityImportConstraint> constraints)
    {
        foreach (var group in constraints.GroupBy(item => item.CharacteristicDefinitionId))
        {
            var equalValues = group.Where(item => item.ComparisonOperator ==
                    ComponentCompatibilityComparisonOperator.Equal)
                .ToArray();
            var lowerBounds = group.Where(item => item.ComparisonOperator ==
                    ComponentCompatibilityComparisonOperator.GreaterThanOrEqual)
                .ToArray();
            var upperBounds = group.Where(item => item.ComparisonOperator ==
                    ComponentCompatibilityComparisonOperator.LessThanOrEqual)
                .ToArray();

            if (lowerBounds.Length > 1 || upperBounds.Length > 1)
            {
                return UnitResult.Failure(new DomainError(
                    "catalog.component_import.invalid_range",
                    "Для характеристики допускается только одна нижняя и одна верхняя граница."));
            }

            if (equalValues.Length > 0
                && (lowerBounds.Length > 0 || upperBounds.Length > 0))
            {
                return UnitResult.Failure(new DomainError(
                    "catalog.component_import.invalid_range",
                    "Для одной характеристики используйте либо точные значения, либо диапазон «от/до»."));
            }

            if (lowerBounds.Length == 1 && upperBounds.Length == 1
                && decimal.TryParse(lowerBounds[0].Value, NumberStyles.Number,
                    CultureInfo.InvariantCulture, out var lowerValue)
                && decimal.TryParse(upperBounds[0].Value, NumberStyles.Number,
                    CultureInfo.InvariantCulture, out var upperValue)
                && lowerValue > upperValue)
            {
                return UnitResult.Failure(new DomainError(
                    "catalog.component_import.invalid_range",
                    "Нижняя граница диапазона не может быть больше верхней."));
            }
        }

        return UnitResult.Success<DomainError>();
    }

    private static Result<T, DomainError> Failure<T>(string message)
    {
        return Result.Failure<T, DomainError>(
            new DomainError("catalog.component_import.invalid", message));
    }

    private sealed record ProductReference(
        Guid Id,
        string Article,
        string ManufacturerName);

    private sealed record ProductTypeReference(Guid Id, string Code, string Name);

    private sealed record NeedReference(
        Guid Id,
        Guid MainProductTypeId,
        string Code,
        string Name);

    private sealed record CharacteristicReference(
        Guid ProductTypeId,
        Guid DefinitionId,
        string Code,
        string Name,
        CharacteristicDataType DataType);

    private sealed record CharacteristicExportReference(
        Guid Id,
        string Code,
        string Name,
        CharacteristicDataType DataType);

    private sealed record CharacteristicColumnReference(
        CharacteristicExportReference Characteristic,
        string Header,
        ComponentCompatibilityComparisonOperator ComparisonOperator);

    private sealed record CharacteristicColumnMetadata(
        string CharacteristicCode,
        ComponentCompatibilityComparisonOperator ComparisonOperator);

    private sealed record PreparedImportRule(
        Guid ComponentProductId,
        Guid MainProductTypeId,
        Guid? NeedDefinitionId,
        bool IsActive,
        IReadOnlyCollection<IReadOnlyCollection<(
            Guid DefinitionId,
            CharacteristicValue Value,
            ComponentCompatibilityComparisonOperator ComparisonOperator)>> Groups);

    private sealed record ReferenceData(
        IReadOnlyCollection<ProductReference> ComponentProducts,
        IReadOnlyCollection<ProductTypeReference> MainProductTypes,
        IReadOnlyCollection<NeedReference> Needs,
        IReadOnlyCollection<CharacteristicReference> Characteristics,
        IReadOnlyDictionary<Guid, CharacteristicDefinition> Definitions,
        IReadOnlyDictionary<
            (Guid ProductId, Guid MainProductTypeId, Guid? NeedId),
            ExistingRuleReference>
            ExistingRules);

    private sealed record ExistingRuleReference(
        bool IsActive,
        IReadOnlySet<string> GroupSignatures);
}
