using System.Globalization;
using ClosedXML.Excel;
using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Characteristics.Normalization;
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

    public byte[] CreateTemplate()
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Правила совместимости");
        var headers = new[]
        {
            "Артикул комплектующего",
            "Тип основного товара",
            "Потребность",
            "Характеристика 1",
            "Характеристика 2"
        };

        for (var index = 0; index < headers.Length; index++)
        {
            worksheet.Cell(1, index + 1).Value = headers[index];
        }

        worksheet.Cell(2, 1).Value = "АРТИКУЛ-001";
        worksheet.Cell(2, 2).Value = "КОНТАКТОР";
        worksheet.Cell(2, 3).Value = "ДОПОЛНИТЕЛЬНЫЙ КОНТАКТ";
        worksheet.Cell(2, 4).Value = "значение; другое значение";
        worksheet.Cell(2, 5).Value = "значение";
        worksheet.Range(1, 1, 1, headers.Length).Style.Font.Bold = true;
        worksheet.Range(1, 1, 1, headers.Length).Style.Fill.BackgroundColor =
            XLColor.FromHtml("#DDEBF7");
        worksheet.SheetView.FreezeRows(1);
        worksheet.Columns().AdjustToContents();
        worksheet.Column(1).Width = Math.Max(worksheet.Column(1).Width, 28);
        worksheet.Column(2).Width = Math.Max(worksheet.Column(2).Width, 28);
        worksheet.Column(3).Width = Math.Max(worksheet.Column(3).Width, 28);

        var instructions = workbook.Worksheets.Add("Инструкция");
        instructions.Cell(1, 1).Value = "Как заполнить шаблон";
        instructions.Cell(1, 1).Style.Font.Bold = true;
        instructions.Cell(3, 1).Value =
            "1. Артикул комплектующего должен уже существовать в каталоге и иметь тип «Комплектующее».";
        instructions.Cell(4, 1).Value =
            "2. Тип основного товара можно указать кодом или названием.";
        instructions.Cell(5, 1).Value =
            "3. Потребность можно указать кодом или названием для выбранного типа товара.";
        instructions.Cell(6, 1).Value =
            "4. Переименуйте колонки «Характеристика 1/2» в реальные коды или названия характеристик.";
        instructions.Cell(7, 1).Value =
            "5. Несколько допустимых значений одной характеристики разделяйте точкой с запятой.";
        instructions.Column(1).Width = 120;
        instructions.Column(1).Style.Alignment.WrapText = true;

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
            var worksheet = workbook.Worksheets.FirstOrDefault();

            if (worksheet is null)
            {
                return Failure<ComponentCompatibilityImportPreview>(
                    "Файл не содержит листов Excel.");
            }

            var headers = ReadHeaders(worksheet);
            var articleColumn = FindColumn(
                headers,
                "АРТИКУЛ КОМПЛЕКТУЮЩЕГО",
                "АРТИКУЛ");
            var mainTypeColumn = FindColumn(headers, "ТИП ОСНОВНОГО ТОВАРА");
            var needColumn = FindColumn(headers, "ПОТРЕБНОСТЬ");

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
            var representedKeys = new HashSet<(Guid ProductId, Guid NeedId)>();
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
                var mainTypeValue = ReadCell(row, mainTypeColumn.Value);
                var needValue = ReadCell(row, needColumn.Value);

                if (article.Length == 0
                    && mainTypeValue.Length == 0
                    && needValue.Length == 0)
                {
                    continue;
                }

                var componentCandidates = referenceData.ComponentProducts
                    .Where(item => string.Equals(
                        Normalize(item.Article),
                        Normalize(article),
                        StringComparison.Ordinal))
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

                var component = componentCandidates[0];
                var need = needCandidates[0];
                var key = (component.Id, need.Id);

                if (!representedKeys.Add(key))
                {
                    rows.Add(ProblemRow(
                        rowNumber,
                        article,
                        mainTypeValue,
                        needValue,
                        ComponentCompatibilityImportRowStatus.Duplicate,
                        "Такое правило уже встречалось выше в файле."));
                    continue;
                }

                if (referenceData.ExistingOffers.Contains(key))
                {
                    rows.Add(new ComponentCompatibilityImportPreviewRow(
                        rowNumber,
                        article,
                        mainType.Name,
                        need.Name,
                        component.Id,
                        need.Id,
                        ComponentCompatibilityImportRowStatus.AlreadyExists,
                        "Правило уже существует и повторно создано не будет.",
                        []));
                    continue;
                }

                var constraintResult = CreateConstraints(
                    row,
                    headers,
                    mainType.Id,
                    referenceData.Characteristics,
                    referenceData.Definitions,
                    articleColumn.Value,
                    mainTypeColumn.Value,
                    needColumn.Value);

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

                rows.Add(new ComponentCompatibilityImportPreviewRow(
                    rowNumber,
                    article,
                    mainType.Name,
                    need.Name,
                    component.Id,
                    need.Id,
                    ComponentCompatibilityImportRowStatus.Ready,
                    null,
                    constraintResult.Value));
            }

            return new ComponentCompatibilityImportPreview(
                rows.Count,
                rows.Count(item =>
                    item.Status == ComponentCompatibilityImportRowStatus.Ready),
                rows.Count(item =>
                    item.Status != ComponentCompatibilityImportRowStatus.Ready),
                rows);
        }
        catch (Exception exception) when (
            exception is not OperationCanceledException)
        {
            return Failure<ComponentCompatibilityImportPreview>(
                "Не удалось прочитать Excel-файл правил совместимости.");
        }
    }

    public async Task<Result<int, DomainError>> ApplyAsync(
        IReadOnlyList<ApplyComponentCompatibilityImportRow> rows,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rows);

        if (rows.Count is 0 or > MaximumRowsCount)
        {
            return Failure<int>("Нет правил для применения или превышен лимит строк.");
        }

        if (rows.Any(row =>
                row.ComponentProductId == Guid.Empty
                || row.NeedDefinitionId == Guid.Empty)
            || rows.GroupBy(row =>
                    (row.ComponentProductId, row.NeedDefinitionId))
                .Any(group => group.Count() > 1))
        {
            return Failure<int>("В запросе есть некорректные или повторяющиеся правила.");
        }

        var productIds = rows.Select(row => row.ComponentProductId).Distinct().ToArray();
        var needIds = rows.Select(row => row.NeedDefinitionId).Distinct().ToArray();
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
        var definitions = await _dbContext.CharacteristicDefinitions
            .Where(item => definitionIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken)
            .ConfigureAwait(false);
        var mainProductTypeIds = needs.Values
            .Select(need => need.MainProductTypeId)
            .Distinct()
            .ToArray();
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
        var existing = await _dbContext.ComponentOffers
            .Where(item => productIds.Contains(item.ComponentProductId)
                && needIds.Contains(item.NeedDefinitionId))
            .Select(item => new { item.ComponentProductId, item.NeedDefinitionId })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var existingKeys = existing
            .Select(item => (item.ComponentProductId, item.NeedDefinitionId))
            .ToHashSet();

        foreach (var row in rows)
        {
            if (!componentIds.Contains(row.ComponentProductId)
                || !needs.TryGetValue(row.NeedDefinitionId, out var need)
                || existingKeys.Contains(
                    (row.ComponentProductId, row.NeedDefinitionId)))
            {
                return Failure<int>(
                    "Состав каталога изменился. Выполните предпросмотр повторно.");
            }

            var offerResult = ComponentOffer.Create(
                row.ComponentProductId,
                row.NeedDefinitionId);

            if (offerResult.IsFailure)
            {
                return Result.Failure<int, DomainError>(offerResult.Error);
            }

            var offer = offerResult.Value;

            foreach (var constraint in row.Constraints)
            {
                if (!definitions.TryGetValue(
                        constraint.CharacteristicDefinitionId,
                        out var definition)
                    || !allowed.Contains(
                        (need.MainProductTypeId, definition.Id)))
                {
                    return Failure<int>(
                        "Характеристика больше не разрешена для типа товара.");
                }

                var valueResult = ParseValue(definition, constraint.Value);

                if (valueResult.IsFailure)
                {
                    return Result.Failure<int, DomainError>(valueResult.Error);
                }

                var addResult = offer.AddConstraint(definition.Id, valueResult.Value);

                if (addResult.IsFailure)
                {
                    return Result.Failure<int, DomainError>(addResult.Error);
                }
            }

            _dbContext.ComponentOffers.Add(offer);
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return rows.Count;
    }

    private async Task<ReferenceData> LoadReferenceDataAsync(
        CancellationToken cancellationToken)
    {
        var componentProducts = await (
                from product in _dbContext.Products.AsNoTracking()
                join productType in _dbContext.ProductTypes.AsNoTracking()
                    on product.ProductTypeId equals productType.Id
                where productType.Kind == ProductTypeKind.Component
                select new ProductReference(product.Id, product.Article.Value))
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
                    definition.Name))
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        var definitions = await _dbContext.CharacteristicDefinitions
            .AsNoTracking()
            .ToDictionaryAsync(item => item.Id, cancellationToken)
            .ConfigureAwait(false);
        var existingOffers = await _dbContext.ComponentOffers
            .AsNoTracking()
            .Select(item => new { item.ComponentProductId, item.NeedDefinitionId })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new ReferenceData(
            componentProducts,
            mainProductTypes,
            needs,
            characteristics,
            definitions,
            existingOffers
                .Select(item => (item.ComponentProductId, item.NeedDefinitionId))
                .ToHashSet());
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
            int needColumn)
    {
        var result = new List<ComponentCompatibilityImportConstraint>();

        foreach (var header in headers)
        {
            if (header.Key == articleColumn
                || header.Key == mainTypeColumn
                || header.Key == needColumn
                || IsIgnoredStandardHeader(header.Value))
            {
                continue;
            }

            var rawValue = ReadCell(row, header.Key);

            if (rawValue.Length == 0)
            {
                continue;
            }

            var candidates = characteristics
                .Where(item =>
                    item.ProductTypeId == mainProductTypeId
                    && (string.Equals(
                            Normalize(item.Code),
                            Normalize(header.Value),
                            StringComparison.Ordinal)
                        || string.Equals(
                            Normalize(item.Name),
                            Normalize(header.Value),
                            StringComparison.Ordinal)))
                .ToArray();

            if (candidates.Length != 1)
            {
                return Failure<IReadOnlyList<ComponentCompatibilityImportConstraint>>(
                    $"Колонка «{header.Value}» не соответствует характеристике "
                    + "выбранного типа основного товара.");
            }

            foreach (var value in rawValue
                         .Split(';', StringSplitOptions.TrimEntries
                             | StringSplitOptions.RemoveEmptyEntries))
            {
                if (!definitions.TryGetValue(
                        candidates[0].DefinitionId,
                        out var definition))
                {
                    return Failure<IReadOnlyList<ComponentCompatibilityImportConstraint>>(
                        $"Характеристика «{candidates[0].Name}» не найдена.");
                }

                var valueResult = ParseValue(definition, value);

                if (valueResult.IsFailure)
                {
                    return Failure<IReadOnlyList<ComponentCompatibilityImportConstraint>>(
                        valueResult.Error.Message);
                }

                result.Add(new ComponentCompatibilityImportConstraint(
                    candidates[0].DefinitionId,
                    candidates[0].Name,
                    value));
            }
        }

        return result;
    }

    private static Result<CharacteristicValue, DomainError> ParseValue(
        CharacteristicDefinition definition,
        string rawValue)
    {
        Result<CharacteristicValue, DomainError> result = definition.DataType switch
        {
            CharacteristicDataType.Text => CharacteristicValue.CreateText(rawValue),
            CharacteristicDataType.Number when decimal.TryParse(
                rawValue.Replace(',', '.'),
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var number) => CharacteristicValue.CreateNumber(number),
            CharacteristicDataType.Boolean
                when CatalogCharacteristicBooleanValueNormalizer.TryNormalize(
                    definition.Code,
                    rawValue,
                    out var boolean) => CharacteristicValue.CreateBoolean(boolean),
            _ => Failure<CharacteristicValue>(
                $"Некорректное значение «{rawValue}» характеристики "
                + $"«{definition.Name}».")
        };

        if (result.IsFailure)
        {
            return result;
        }

        var validationResult = definition.ValidateValue(result.Value);

        return validationResult.IsSuccess
            ? result
            : Result.Failure<CharacteristicValue, DomainError>(
                validationResult.Error);
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
        return Normalize(header) is "НАИМЕНОВАНИЕ"
            or "ПРОИЗВОДИТЕЛЬ"
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
            status,
            message,
            []);
    }

    private static Result<T, DomainError> Failure<T>(string message)
    {
        return Result.Failure<T, DomainError>(
            new DomainError("catalog.component_import.invalid", message));
    }

    private sealed record ProductReference(Guid Id, string Article);

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
        string Name);

    private sealed record ReferenceData(
        IReadOnlyCollection<ProductReference> ComponentProducts,
        IReadOnlyCollection<ProductTypeReference> MainProductTypes,
        IReadOnlyCollection<NeedReference> Needs,
        IReadOnlyCollection<CharacteristicReference> Characteristics,
        IReadOnlyDictionary<Guid, CharacteristicDefinition> Definitions,
        IReadOnlySet<(Guid ProductId, Guid NeedId)> ExistingOffers);
}
