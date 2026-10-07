using System.Globalization;
using ClosedXML.Excel;
using ElectronicService.Core.Catalog.PriceCalculations.Import;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.ProductTypes;
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

    private static readonly string[] LineIdHeaders =
        ["ID СТРОКИ", "LINE ID"];

    private static readonly string[] ProductIdHeaders =
        ["ID ТОВАРА", "PRODUCT ID"];

    private readonly ElectronicDbContext _dbContext;

    public CatalogPriceCalculationWorkbookPreviewer(
        ElectronicDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public async Task<CatalogPriceCalculationImportPreview>
        PreviewAsync(
            Guid calculationId,
            Stream workbookStream,
            string fileName,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workbookStream);

        if (calculationId == Guid.Empty)
        {
            throw new InvalidDataException("Не указан проект расчёта.");
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
            var metadata = ReadMetadata(workbook, calculationId);
            var worksheet =
                workbook.Worksheets.FirstOrDefault(item =>
                    string.Equals(
                        item.Name,
                        "Позиции",
                        StringComparison.OrdinalIgnoreCase))
                ?? workbook.Worksheets.FirstOrDefault()
                ?? throw new InvalidDataException(
                    "В файле нет листов.");

            var existingLines = await _dbContext.CatalogPriceCalculationLines
                .AsNoTracking()
                .Where(line => line.CalculationId == calculationId)
                .Select(line => new ExistingCalculationLine(
                    line.Id,
                    line.ProductId,
                    line.ManufacturerId,
                    line.PriceListId,
                    line.PriceListRowId,
                    line.Article,
                    line.Name,
                    line.ManufacturerName,
                    line.Quantity,
                    line.BasePriceAmount,
                    line.MrcPriceAmount))
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);

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

            var componentRows = metadata.IsProjectWorkbook
                ? await CreateComponentPreviewAsync(
                        workbook,
                        existingLines,
                        cancellationToken)
                    .ConfigureAwait(false)
                : [];

            var characteristicRows = metadata.IsProjectWorkbook
                ? await CreateCharacteristicPreviewAsync(
                        workbook,
                        cancellationToken)
                    .ConfigureAwait(false)
                : [];

            if (parsedWorkbook.ValidRows.Count == 0)
            {
                return CreatePreview(
                    parsedWorkbook.ReadRowsCount,
                    parsedWorkbook.InvalidRows,
                    metadata.IsProjectWorkbook,
                    metadata.Warning,
                    componentRows,
                    characteristicRows);
            }

            var normalizedArticles =
                parsedWorkbook.ValidRows
                    .Select(row => row.NormalizedArticle)
                    .Distinct(StringComparer.Ordinal)
                    .ToHashSet(StringComparer.Ordinal);

            var sourceProductIds = parsedWorkbook.ValidRows
                .Where(row => row.SourceProductId.HasValue)
                .Select(row => row.SourceProductId!.Value)
                .ToHashSet();

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
                        product.StockQuantity.Value,
                        product.Price.Amount))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var productReferences =
                allProductReferences
                    .Where(
                        product =>
                            normalizedArticles.Contains(
                                NormalizeArticle(
                                    product.Article))
                            || sourceProductIds.Contains(product.ProductId))
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

            var productsById = productReferences
                .ToDictionary(product => product.ProductId);

            var existingLinesById = existingLines
                .ToDictionary(line => line.LineId);

            var existingLinesByProductId = existingLines
                .GroupBy(line => line.ProductId)
                .ToDictionary(group => group.Key, group => group.ToArray());

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
                            row.ProductId!.Value,
                            priceList.ManufacturerId,
                            priceList.Id,
                            row.Id,
                            row.BasePriceAmount!.Value,
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

                ProductReference[] articleProducts;

                if (row.SourceProductId.HasValue
                    && productsById.TryGetValue(
                        row.SourceProductId.Value,
                        out var productById))
                {
                    articleProducts = [productById];
                }
                else if (!productsByArticle.TryGetValue(
                             row.NormalizedArticle,
                             out articleProducts!))
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

                var productPrices = pricesByProduct.GetValueOrDefault(
                    product.ProductId,
                    [])
                    .Where(price =>
                        price.ManufacturerId == product.ManufacturerId)
                    .ToArray();
                var price = productPrices.Length == 1
                    ? productPrices[0]
                    : new PriceReference(
                        product.ProductId,
                        product.ManufacturerId,
                        null,
                        null,
                        product.CatalogPriceAmount,
                        null);

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

                ExistingCalculationLine? existingLine = null;

                if (row.SourceLineId.HasValue)
                {
                    existingLinesById.TryGetValue(
                        row.SourceLineId.Value,
                        out existingLine);
                }

                if (existingLine is null
                    && existingLinesByProductId.TryGetValue(
                        product.ProductId,
                        out var productLines)
                    && productLines.Length == 1)
                {
                    existingLine = productLines[0];
                }

                var status = CatalogPriceCalculationImportRowStatus.New;

                if (existingLine is not null)
                {
                    status = existingLine.Quantity == row.Quantity
                        ? CatalogPriceCalculationImportRowStatus.Unchanged
                        : CatalogPriceCalculationImportRowStatus.QuantityChanged;
                }

                if (status == CatalogPriceCalculationImportRowStatus.QuantityChanged)
                {
                    message = $"Количество изменится: {existingLine!.Quantity:N3} → {row.Quantity:N3}.";
                }
                else if (status == CatalogPriceCalculationImportRowStatus.Unchanged)
                {
                    message = "Позиция уже есть в проекте, изменений нет.";
                }

                previewRows.Add(
                    new CatalogPriceCalculationImportPreviewRow(
                        row.RowNumber,
                        row.Article,
                        row.SourceName,
                        row.SourceManufacturer,
                        row.Quantity,
                        existingLine?.LineId,
                        existingLine?.Quantity,
                        status,
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

            if (metadata.IsProjectWorkbook)
            {
                var representedLineIds = parsedWorkbook.ValidRows
                    .Where(row => row.SourceLineId.HasValue)
                    .Select(row => row.SourceLineId!.Value)
                    .ToHashSet();

                var representedProductIds = previewRows
                    .Where(row => row.ProductId.HasValue)
                    .Select(row => row.ProductId!.Value)
                    .ToHashSet();

                var syntheticRowNumber = lastRowNumber;

                foreach (var existingLine in existingLines)
                {
                    if (representedLineIds.Contains(existingLine.LineId)
                        || representedProductIds.Contains(existingLine.ProductId))
                    {
                        continue;
                    }

                    syntheticRowNumber++;
                    previewRows.Add(
                        new CatalogPriceCalculationImportPreviewRow(
                            syntheticRowNumber,
                            existingLine.Article,
                            existingLine.Name,
                            existingLine.ManufacturerName,
                            null,
                            existingLine.LineId,
                            existingLine.Quantity,
                            CatalogPriceCalculationImportRowStatus.Removed,
                            "Позиция отсутствует в загруженной выгрузке и будет удалена после подтверждения.",
                            existingLine.ProductId,
                            existingLine.Article,
                            existingLine.Name,
                            existingLine.ManufacturerId,
                            existingLine.ManufacturerName,
                            null,
                            null,
                            existingLine.PriceListId,
                            existingLine.PriceListRowId,
                            existingLine.BasePriceAmount,
                            existingLine.MrcPriceAmount));
                }
            }

            return CreatePreview(
                parsedWorkbook.ReadRowsCount,
                previewRows,
                metadata.IsProjectWorkbook,
                metadata.Warning,
                componentRows,
                characteristicRows);
        }
    }

    private static CatalogPriceCalculationImportPreview
        CreatePreview(
            int readRowsCount,
            IReadOnlyCollection<
                CatalogPriceCalculationImportPreviewRow> rows,
            bool isProjectWorkbook,
            string? warning,
            IReadOnlyCollection<CatalogPriceCalculationComponentImportPreviewRow>? componentRows = null,
            IReadOnlyCollection<CatalogPriceCalculationCharacteristicImportPreviewRow>? characteristicRows = null)
    {
        var orderedRows =
            rows
                .OrderBy(row => row.RowNumber)
                .ToArray();

        var matchedRowsCount =
            orderedRows.Count(
                row =>
                    row.Status is
                        CatalogPriceCalculationImportRowStatus.New
                        or CatalogPriceCalculationImportRowStatus.QuantityChanged
                        or CatalogPriceCalculationImportRowStatus.Removed);

        var addedRowsCount = orderedRows.Count(row =>
            row.Status == CatalogPriceCalculationImportRowStatus.New);
        var updatedRowsCount = orderedRows.Count(row =>
            row.Status == CatalogPriceCalculationImportRowStatus.QuantityChanged);
        var removedRowsCount = orderedRows.Count(row =>
            row.Status == CatalogPriceCalculationImportRowStatus.Removed);
        var unchangedRowsCount = orderedRows.Count(row =>
            row.Status == CatalogPriceCalculationImportRowStatus.Unchanged);
        var skippedRowsCount = orderedRows.Count(row =>
            row.Status is not CatalogPriceCalculationImportRowStatus.New
                and not CatalogPriceCalculationImportRowStatus.QuantityChanged
                and not CatalogPriceCalculationImportRowStatus.Removed
                and not CatalogPriceCalculationImportRowStatus.Unchanged);

        return new CatalogPriceCalculationImportPreview(
            readRowsCount,
            matchedRowsCount,
            skippedRowsCount,
            addedRowsCount,
            updatedRowsCount,
            removedRowsCount,
            unchangedRowsCount,
            isProjectWorkbook,
            warning,
            orderedRows,
            componentRows?
                .OrderBy(row => row.RowNumber)
                .ToArray()
            ?? [],
            characteristicRows?
                .OrderBy(row => row.RowNumber)
                .ToArray()
            ?? []);
    }

    private async Task<IReadOnlyList<CatalogPriceCalculationComponentImportPreviewRow>>
        CreateComponentPreviewAsync(
            XLWorkbook workbook,
            IReadOnlyCollection<ExistingCalculationLine> existingLines,
            CancellationToken cancellationToken)
    {
        var worksheet = workbook.Worksheets.FirstOrDefault(item =>
            string.Equals(
                item.Name,
                "Комплектующие",
                StringComparison.OrdinalIgnoreCase));

        if (worksheet is null)
        {
            return [];
        }

        var headerRow = 1;
        var mainArticleColumn = FindColumn(
            worksheet,
            headerRow,
            "АРТИКУЛ ОСНОВНОГО ТОВАРА");
        var needNameColumn = FindColumn(worksheet, headerRow, "ПОТРЕБНОСТЬ");
        var componentArticleColumn = FindColumn(
            worksheet,
            headerRow,
            "АРТИКУЛ КОМПЛЕКТУЮЩЕГО");
        var quantityColumn = FindColumn(worksheet, headerRow, "НА ЕДИНИЦУ");
        var mainLineIdColumn = FindColumn(
            worksheet,
            headerRow,
            "ID СТРОКИ ОСНОВНОГО ТОВАРА");
        var componentLineIdColumn = FindColumn(
            worksheet,
            headerRow,
            "ID СТРОКИ КОМПЛЕКТУЮЩЕГО");
        var needIdColumn = FindColumn(
            worksheet,
            headerRow,
            "ID ПОТРЕБНОСТИ");
        var componentProductIdColumn = FindColumn(
            worksheet,
            headerRow,
            "ID ТОВАРА КОМПЛЕКТУЮЩЕГО");

        if (!mainArticleColumn.HasValue
            || !needNameColumn.HasValue
            || !componentArticleColumn.HasValue
            || !quantityColumn.HasValue)
        {
            return [];
        }

        var lineIds = existingLines.Select(line => line.LineId).ToArray();
        var existingComponents = lineIds.Length == 0
            ? []
            : await _dbContext.CatalogPriceCalculationLineComponents
                .AsNoTracking()
                .Where(component => lineIds.Contains(component.CalculationLineId))
                .Select(component => new ExistingCalculationComponent(
                    component.Id,
                    component.CalculationLineId,
                    component.NeedDefinitionId,
                    component.NeedName,
                    component.ComponentProductId,
                    component.Article,
                    component.QuantityPerUnit))
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);

        var mainProductIds = existingLines
            .Select(line => line.ProductId)
            .Distinct()
            .ToArray();
        var mainProductTypes = await _dbContext.Products
            .AsNoTracking()
            .Where(product => mainProductIds.Contains(product.Id))
            .Select(product => new { product.Id, product.ProductTypeId })
            .ToDictionaryAsync(
                product => product.Id,
                product => product.ProductTypeId,
                cancellationToken)
            .ConfigureAwait(false);

        var mainProductTypeIds = mainProductTypes.Values.Distinct().ToArray();
        var needs = await _dbContext.ComponentNeedDefinitions
            .AsNoTracking()
            .Where(need => mainProductTypeIds.Contains(need.MainProductTypeId))
            .Select(need => new ComponentNeedReference(
                need.Id,
                need.MainProductTypeId,
                need.Name))
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        var componentProducts = await (
                from product in _dbContext.Products.AsNoTracking()
                join productType in _dbContext.ProductTypes.AsNoTracking()
                    on product.ProductTypeId equals productType.Id
                where productType.Kind == ProductTypeKind.Component
                select new ComponentProductReference(
                    product.Id,
                    product.ManufacturerId,
                    product.Article.Value))
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        var existingLinesById = existingLines.ToDictionary(line => line.LineId);
        var existingLinesByArticle = existingLines
            .GroupBy(
                line => NormalizeArticle(line.Article),
                StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.ToArray(),
                StringComparer.Ordinal);
        var existingComponentsById = existingComponents
            .ToDictionary(component => component.ComponentLineId);
        var productsById = componentProducts
            .ToDictionary(product => product.ProductId);
        var productsByArticle = componentProducts
            .GroupBy(
                product => NormalizeArticle(product.Article),
                StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.ToArray(),
                StringComparer.Ordinal);
        var needsById = needs.ToDictionary(need => need.NeedDefinitionId);
        var result = new List<CatalogPriceCalculationComponentImportPreviewRow>();
        var representedComponentLineIds = new HashSet<Guid>();
        var representedKeys = new HashSet<(Guid MainLineId, Guid NeedId, Guid ProductId)>();
        var lastRowNumber = worksheet.LastRowUsed()?.RowNumber() ?? headerRow;

        for (var rowNumber = headerRow + 1;
             rowNumber <= lastRowNumber;
             rowNumber++)
        {
            var row = worksheet.Row(rowNumber);
            var mainArticle = row.Cell(mainArticleColumn.Value)
                .GetFormattedString().Trim();
            var needName = row.Cell(needNameColumn.Value)
                .GetFormattedString().Trim();
            var componentArticle = row.Cell(componentArticleColumn.Value)
                .GetFormattedString().Trim();
            var quantityText = row.Cell(quantityColumn.Value)
                .GetFormattedString().Trim();

            if (mainArticle.Length == 0
                && needName.Length == 0
                && componentArticle.Length == 0
                && quantityText.Length == 0)
            {
                continue;
            }

            var mainLineId = ReadOptionalGuid(row, mainLineIdColumn);
            var componentLineId = ReadOptionalGuid(row, componentLineIdColumn);
            var needId = ReadOptionalGuid(row, needIdColumn);
            var componentProductId = ReadOptionalGuid(row, componentProductIdColumn);
            var quantityValid = int.TryParse(
                quantityText,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var quantityPerUnit)
                && quantityPerUnit >= 1
                && quantityPerUnit <= CatalogPriceCalculationLineComponent.MaximumQuantityPerUnit;

            if (!quantityValid)
            {
                result.Add(CreateComponentProblemRow(
                    rowNumber,
                    mainArticle,
                    needName,
                    componentArticle,
                    null,
                    mainLineId,
                    componentLineId,
                    needId,
                    componentProductId,
                    CatalogPriceCalculationComponentImportRowStatus.Invalid,
                    "Количество на единицу должно быть целым числом больше нуля."));
                continue;
            }

            ExistingCalculationLine? mainLine = null;

            if (mainLineId.HasValue)
            {
                existingLinesById.TryGetValue(mainLineId.Value, out mainLine);
            }

            if (mainLine is null
                && existingLinesByArticle.TryGetValue(
                    NormalizeArticle(mainArticle),
                    out var articleLines)
                && articleLines.Length == 1)
            {
                mainLine = articleLines[0];
                mainLineId = mainLine.LineId;
            }

            if (mainLine is null)
            {
                result.Add(CreateComponentProblemRow(
                    rowNumber,
                    mainArticle,
                    needName,
                    componentArticle,
                    quantityPerUnit,
                    mainLineId,
                    componentLineId,
                    needId,
                    componentProductId,
                    CatalogPriceCalculationComponentImportRowStatus.MainLineNotFound,
                    "Основная позиция проекта не найдена."));
                continue;
            }

            ComponentProductReference? componentProduct = null;

            if (componentProductId.HasValue)
            {
                productsById.TryGetValue(componentProductId.Value, out componentProduct);
            }

            if (componentProduct is null
                && productsByArticle.TryGetValue(
                    NormalizeArticle(componentArticle),
                    out var articleProducts)
                && articleProducts.Length == 1)
            {
                componentProduct = articleProducts[0];
                componentProductId = componentProduct.ProductId;
            }

            if (componentProduct is null)
            {
                result.Add(CreateComponentProblemRow(
                    rowNumber,
                    mainArticle,
                    needName,
                    componentArticle,
                    quantityPerUnit,
                    mainLine.LineId,
                    componentLineId,
                    needId,
                    componentProductId,
                    CatalogPriceCalculationComponentImportRowStatus.ComponentNotFound,
                    "Комплектующее не найдено в каталоге."));
                continue;
            }

            ComponentNeedReference? need = null;

            if (needId.HasValue)
            {
                needsById.TryGetValue(needId.Value, out need);
            }

            if (need is null
                && mainProductTypes.TryGetValue(mainLine.ProductId, out var mainTypeId))
            {
                need = needs.SingleOrDefault(candidate =>
                    candidate.MainProductTypeId == mainTypeId
                    && string.Equals(
                        NormalizeText(candidate.Name),
                        NormalizeText(needName),
                        StringComparison.Ordinal));
                needId = need?.NeedDefinitionId;
            }

            if (need is null)
            {
                result.Add(CreateComponentProblemRow(
                    rowNumber,
                    mainArticle,
                    needName,
                    componentArticle,
                    quantityPerUnit,
                    mainLine.LineId,
                    componentLineId,
                    needId,
                    componentProduct.ProductId,
                    CatalogPriceCalculationComponentImportRowStatus.NeedNotFound,
                    "Потребность комплектности не найдена для типа основного товара."));
                continue;
            }

            ExistingCalculationComponent? existingComponent = null;

            if (componentLineId.HasValue)
            {
                existingComponentsById.TryGetValue(
                    componentLineId.Value,
                    out existingComponent);
            }

            if (existingComponent is null)
            {
                existingComponent = existingComponents.SingleOrDefault(candidate =>
                    candidate.MainLineId == mainLine.LineId
                    && candidate.NeedDefinitionId == need.NeedDefinitionId
                    && candidate.ComponentProductId == componentProduct.ProductId);
            }

            var status = CatalogPriceCalculationComponentImportRowStatus.New;
            string? message = null;

            if (existingComponent is not null)
            {
                componentLineId = existingComponent.ComponentLineId;
                representedComponentLineIds.Add(existingComponent.ComponentLineId);
                status = existingComponent.QuantityPerUnit == quantityPerUnit
                    ? CatalogPriceCalculationComponentImportRowStatus.Unchanged
                    : CatalogPriceCalculationComponentImportRowStatus.QuantityChanged;
                message = status == CatalogPriceCalculationComponentImportRowStatus.Unchanged
                    ? "Комплектующее уже есть в проекте, изменений нет."
                    : $"Количество на единицу изменится: {existingComponent.QuantityPerUnit} → {quantityPerUnit}.";
            }

            representedKeys.Add((
                mainLine.LineId,
                need.NeedDefinitionId,
                componentProduct.ProductId));
            result.Add(new CatalogPriceCalculationComponentImportPreviewRow(
                rowNumber,
                mainArticle,
                need.Name,
                componentProduct.Article,
                quantityPerUnit,
                mainLine.LineId,
                componentLineId,
                need.NeedDefinitionId,
                componentProduct.ProductId,
                existingComponent?.QuantityPerUnit,
                status,
                message));
        }

        var syntheticRowNumber = lastRowNumber;

        foreach (var existingComponent in existingComponents)
        {
            var key = (
                existingComponent.MainLineId,
                existingComponent.NeedDefinitionId,
                existingComponent.ComponentProductId);

            if (representedComponentLineIds.Contains(existingComponent.ComponentLineId)
                || representedKeys.Contains(key))
            {
                continue;
            }

            syntheticRowNumber++;
            var mainLine = existingLinesById[existingComponent.MainLineId];
            result.Add(new CatalogPriceCalculationComponentImportPreviewRow(
                syntheticRowNumber,
                mainLine.Article,
                existingComponent.NeedName,
                existingComponent.Article,
                null,
                existingComponent.MainLineId,
                existingComponent.ComponentLineId,
                existingComponent.NeedDefinitionId,
                existingComponent.ComponentProductId,
                existingComponent.QuantityPerUnit,
                CatalogPriceCalculationComponentImportRowStatus.Removed,
                "Комплектующее отсутствует в загруженной выгрузке и будет удалено после подтверждения."));
        }

        return result;
    }

    private async Task<IReadOnlyList<CatalogPriceCalculationCharacteristicImportPreviewRow>>
        CreateCharacteristicPreviewAsync(
            XLWorkbook workbook,
            CancellationToken cancellationToken)
    {
        var worksheet = workbook.Worksheets.FirstOrDefault(item =>
            string.Equals(
                item.Name,
                "Характеристики",
                StringComparison.OrdinalIgnoreCase));

        if (worksheet is null)
        {
            return [];
        }

        const int headerRow = 1;
        var productIdColumn = FindColumn(worksheet, headerRow, "ID ТОВАРА");
        var articleColumn = FindColumn(worksheet, headerRow, "АРТИКУЛ");
        var characteristicCodeColumn = FindColumn(
            worksheet,
            headerRow,
            "КОД ХАРАКТЕРИСТИКИ");
        var valueColumn = FindColumn(worksheet, headerRow, "ЗНАЧЕНИЕ");
        var characteristicColumns = ReadCharacteristicColumnMappings(workbook);
        var isLegacyLayout = characteristicCodeColumn.HasValue
            && valueColumn.HasValue;

        if (!productIdColumn.HasValue
            || !articleColumn.HasValue
            || (!isLegacyLayout && characteristicColumns.Count == 0))
        {
            return
            [
                new CatalogPriceCalculationCharacteristicImportPreviewRow(
                    headerRow,
                    null,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    null,
                    false,
                    null,
                    null,
                    CatalogPriceCalculationCharacteristicImportRowStatus.Invalid,
                    "Не удалось прочитать структуру листа «Характеристики». Выгрузите проект заново и перенесите изменения в новый файл.")
            ];
        }

        var parsedRows = new List<ParsedCharacteristicRow>();
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? headerRow;

        for (var rowNumber = headerRow + 1; rowNumber <= lastRow; rowNumber++)
        {
            var row = worksheet.Row(rowNumber);
            var productIdText = row.Cell(productIdColumn.Value)
                .GetFormattedString().Trim();
            var article = row.Cell(articleColumn.Value)
                .GetFormattedString().Trim();
            if (isLegacyLayout)
            {
                var code = row.Cell(characteristicCodeColumn!.Value)
                    .GetFormattedString().Trim();
                var value = row.Cell(valueColumn!.Value)
                    .GetFormattedString().Trim();

                if (productIdText.Length == 0
                    && article.Length == 0
                    && code.Length == 0
                    && value.Length == 0)
                {
                    continue;
                }

                parsedRows.Add(new ParsedCharacteristicRow(
                    rowNumber,
                    Guid.TryParse(productIdText, out var legacyProductId)
                        ? legacyProductId
                        : null,
                    article,
                    code,
                    value));
                continue;
            }

            var mappedValues = characteristicColumns
                .Select(column => new
                {
                    Column = column,
                    Value = row.Cell(column.ColumnNumber)
                        .GetFormattedString()
                        .Trim()
                })
                .ToArray();

            if (productIdText.Length == 0
                && article.Length == 0
                && mappedValues.All(item => item.Value.Length == 0))
            {
                continue;
            }

            var parsedProductId = Guid.TryParse(productIdText, out var productId)
                ? productId
                : (Guid?)null;

            parsedRows.AddRange(mappedValues.Select(item =>
                new ParsedCharacteristicRow(
                    rowNumber,
                    parsedProductId,
                    article,
                    item.Column.Code,
                    item.Value)));
        }

        if (parsedRows.Count == 0)
        {
            return [];
        }

        var productIds = parsedRows
            .Where(item => item.ProductId.HasValue)
            .Select(item => item.ProductId!.Value)
            .Distinct()
            .ToArray();

        var products = await (
                from product in _dbContext.Products.AsNoTracking()
                join productType in _dbContext.ProductTypes.AsNoTracking()
                    on product.ProductTypeId equals productType.Id
                where productIds.Contains(product.Id)
                select new CharacteristicProductReference(
                    product.Id,
                    product.Article.Value,
                    product.Name.Value,
                    productType.Id,
                    productType.Name))
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        var productTypeIds = products
            .Select(item => item.ProductTypeId)
            .Distinct()
            .ToArray();
        var definitions = await (
                from relation in _dbContext.ProductTypeCharacteristics.AsNoTracking()
                join definition in _dbContext.CharacteristicDefinitions.AsNoTracking()
                    on relation.CharacteristicDefinitionId equals definition.Id
                where productTypeIds.Contains(relation.ProductTypeId)
                select new CharacteristicDefinitionReference(
                    relation.ProductTypeId,
                    definition.Id,
                    definition.Code,
                    definition.Name,
                    definition.DataType,
                    definition.Unit,
                    relation.IsRequired))
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        var currentValues = await _dbContext.ProductCharacteristics
            .AsNoTracking()
            .Where(item => productIds.Contains(item.ProductId))
            .Select(item => new CurrentCharacteristicValue(
                item.ProductId,
                item.CharacteristicDefinitionId,
                item.Value.DataType,
                item.Value.TextValue,
                item.Value.NumberValue,
                item.Value.BooleanValue))
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        var productsById = products.ToDictionary(item => item.ProductId);
        var definitionsByKey = definitions.ToDictionary(
            item => (item.ProductTypeId, NormalizeText(item.Code)));
        var valuesByKey = currentValues.ToDictionary(
            item => (item.ProductId, item.DefinitionId));
        var repeatedKeys = parsedRows
            .Where(item => item.ProductId.HasValue)
            .GroupBy(item => (
                item.ProductId!.Value,
                NormalizeText(item.CharacteristicCode)))
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet();
        var result = new List<CatalogPriceCalculationCharacteristicImportPreviewRow>();

        foreach (var row in parsedRows)
        {
            if (!row.ProductId.HasValue
                || !productsById.TryGetValue(row.ProductId.Value, out var product))
            {
                result.Add(CreateCharacteristicProblemRow(
                    row,
                    CatalogPriceCalculationCharacteristicImportRowStatus.ProductNotFound,
                    "Товар из строки характеристик не найден в каталоге."));
                continue;
            }

            if (!definitionsByKey.TryGetValue(
                    (product.ProductTypeId, NormalizeText(row.CharacteristicCode)),
                    out var definition))
            {
                if (string.IsNullOrWhiteSpace(row.Value))
                {
                    continue;
                }

                result.Add(CreateCharacteristicProblemRow(
                    row,
                    CatalogPriceCalculationCharacteristicImportRowStatus.CharacteristicNotFound,
                    "Характеристика не относится к типу этого товара.",
                    product));
                continue;
            }

            if (repeatedKeys.Contains((
                    product.ProductId,
                    NormalizeText(row.CharacteristicCode))))
            {
                result.Add(CreateCharacteristicRow(
                    row,
                    product,
                    definition,
                    null,
                    row.Value,
                    CatalogPriceCalculationCharacteristicImportRowStatus.Invalid,
                    "Характеристика товара повторяется в файле."));
                continue;
            }

            valuesByKey.TryGetValue(
                (product.ProductId, definition.DefinitionId),
                out var currentValue);
            var normalizedCurrentValue = currentValue is null
                ? null
                : NormalizeCharacteristicValue(
                    currentValue.DataType,
                    currentValue.TextValue,
                    currentValue.NumberValue,
                    currentValue.BooleanValue);

            if (string.IsNullOrWhiteSpace(row.Value))
            {
                if (definition.IsRequired && normalizedCurrentValue is not null)
                {
                    result.Add(CreateCharacteristicRow(
                        row,
                        product,
                        definition,
                        normalizedCurrentValue,
                        null,
                        CatalogPriceCalculationCharacteristicImportRowStatus.Invalid,
                        "Обязательную характеристику нельзя очистить."));
                }
                else if (normalizedCurrentValue is null)
                {
                    result.Add(CreateCharacteristicRow(
                        row,
                        product,
                        definition,
                        null,
                        null,
                        CatalogPriceCalculationCharacteristicImportRowStatus.Unchanged,
                        "Значение не было задано, изменений нет."));
                }
                else
                {
                    result.Add(CreateCharacteristicRow(
                        row,
                        product,
                        definition,
                        normalizedCurrentValue,
                        null,
                        CatalogPriceCalculationCharacteristicImportRowStatus.Removed,
                        "Значение характеристики будет удалено."));
                }

                continue;
            }

            var normalizedNewValue = NormalizeCharacteristicInput(
                definition.DataType,
                row.Value);

            if (normalizedNewValue is null)
            {
                result.Add(CreateCharacteristicRow(
                    row,
                    product,
                    definition,
                    normalizedCurrentValue,
                    row.Value,
                    CatalogPriceCalculationCharacteristicImportRowStatus.Invalid,
                    "Значение не соответствует типу характеристики."));
                continue;
            }

            var status = string.Equals(
                normalizedCurrentValue,
                normalizedNewValue,
                StringComparison.Ordinal)
                ? CatalogPriceCalculationCharacteristicImportRowStatus.Unchanged
                : CatalogPriceCalculationCharacteristicImportRowStatus.Changed;
            var message = "Значение не изменилось.";

            if (status == CatalogPriceCalculationCharacteristicImportRowStatus.Changed)
            {
                message = normalizedCurrentValue is null
                    ? "Значение характеристики будет добавлено."
                    : $"Значение изменится: {normalizedCurrentValue} → {normalizedNewValue}.";
            }

            result.Add(CreateCharacteristicRow(
                row,
                product,
                definition,
                normalizedCurrentValue,
                normalizedNewValue,
                status,
                message));
        }

        return result;
    }

    private static List<CharacteristicColumnMapping>
        ReadCharacteristicColumnMappings(XLWorkbook workbook)
    {
        var worksheet = workbook.Worksheets.FirstOrDefault(item =>
            string.Equals(
                item.Name,
                "_Metadata",
                StringComparison.OrdinalIgnoreCase));

        if (worksheet is null)
        {
            return [];
        }

        var result = new List<CharacteristicColumnMapping>();
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;

        for (var rowNumber = 6; rowNumber <= lastRow; rowNumber++)
        {
            if (!string.Equals(
                    worksheet.Cell(rowNumber, 1).GetFormattedString().Trim(),
                    "CharacteristicColumn",
                    StringComparison.Ordinal))
            {
                continue;
            }

            var columnNumberText = worksheet.Cell(rowNumber, 2)
                .GetFormattedString()
                .Trim();
            var code = worksheet.Cell(rowNumber, 3)
                .GetFormattedString()
                .Trim();

            if (!int.TryParse(
                    columnNumberText,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var columnNumber)
                || columnNumber <= 0
                || code.Length == 0)
            {
                continue;
            }

            result.Add(new CharacteristicColumnMapping(columnNumber, code));
        }

        return result;
    }

    private static CatalogPriceCalculationCharacteristicImportPreviewRow
        CreateCharacteristicProblemRow(
            ParsedCharacteristicRow row,
            CatalogPriceCalculationCharacteristicImportRowStatus status,
            string message,
            CharacteristicProductReference? product = null)
    {
        return new CatalogPriceCalculationCharacteristicImportPreviewRow(
            row.RowNumber,
            row.ProductId,
            row.Article,
            product?.Name ?? string.Empty,
            product?.ProductTypeName ?? string.Empty,
            row.CharacteristicCode,
            string.Empty,
            string.Empty,
            null,
            false,
            null,
            row.Value,
            status,
            message);
    }

    private static CatalogPriceCalculationCharacteristicImportPreviewRow
        CreateCharacteristicRow(
            ParsedCharacteristicRow row,
            CharacteristicProductReference product,
            CharacteristicDefinitionReference definition,
            string? currentValue,
            string? newValue,
            CatalogPriceCalculationCharacteristicImportRowStatus status,
            string message)
    {
        return new CatalogPriceCalculationCharacteristicImportPreviewRow(
            row.RowNumber,
            product.ProductId,
            product.Article,
            product.Name,
            product.ProductTypeName,
            definition.Code,
            definition.Name,
            definition.DataType.ToString(),
            definition.Unit,
            definition.IsRequired,
            currentValue,
            newValue,
            status,
            message);
    }

    private static string? NormalizeCharacteristicInput(
        CharacteristicDataType dataType,
        string value)
    {
        var trimmedValue = value.Trim();

        if (dataType == CharacteristicDataType.Text)
        {
            return trimmedValue.Length == 0 ? null : trimmedValue;
        }

        if (dataType == CharacteristicDataType.Number)
        {
            var normalized = trimmedValue
                .Replace(" ", string.Empty, StringComparison.Ordinal)
                .Replace(",", ".", StringComparison.Ordinal);

            return decimal.TryParse(
                normalized,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var number)
                ? number.ToString("G29", CultureInfo.InvariantCulture)
                : null;
        }

        if (dataType == CharacteristicDataType.Boolean)
        {
            var normalized = NormalizeText(trimmedValue);

            if (normalized is "TRUE" or "ДА" or "ЕСТЬ" or "1" or "+")
            {
                return "True";
            }

            if (normalized is "FALSE" or "НЕТ" or "ОТСУТСТВУЕТ" or "0" or "-")
            {
                return "False";
            }
        }

        return null;
    }

    private static string? NormalizeCharacteristicValue(
        CharacteristicDataType dataType,
        string? textValue,
        decimal? numberValue,
        bool? booleanValue)
    {
        return dataType switch
        {
            CharacteristicDataType.Text => textValue,
            CharacteristicDataType.Number => numberValue?.ToString(
                "G29",
                CultureInfo.InvariantCulture),
            CharacteristicDataType.Boolean => booleanValue?.ToString(
                CultureInfo.InvariantCulture),
            _ => null
        };
    }

    private static int? FindColumn(
        IXLWorksheet worksheet,
        int headerRow,
        string normalizedHeader)
    {
        var lastColumn = worksheet.Row(headerRow)
            .LastCellUsed()?.Address.ColumnNumber ?? 0;

        for (var column = 1; column <= lastColumn; column++)
        {
            if (string.Equals(
                    NormalizeHeader(worksheet.Cell(headerRow, column)
                        .GetFormattedString()),
                    normalizedHeader,
                    StringComparison.Ordinal))
            {
                return column;
            }
        }

        return null;
    }

    private static CatalogPriceCalculationComponentImportPreviewRow
        CreateComponentProblemRow(
            int rowNumber,
            string mainArticle,
            string needName,
            string componentArticle,
            int? quantityPerUnit,
            Guid? mainLineId,
            Guid? componentLineId,
            Guid? needId,
            Guid? componentProductId,
            CatalogPriceCalculationComponentImportRowStatus status,
            string message)
    {
        return new CatalogPriceCalculationComponentImportPreviewRow(
            rowNumber,
            mainArticle,
            needName,
            componentArticle,
            quantityPerUnit,
            mainLineId,
            componentLineId,
            needId,
            componentProductId,
            null,
            status,
            message);
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
            var sourceLineId = ReadOptionalGuid(
                row,
                header.LineIdColumn);
            var sourceProductId = ReadOptionalGuid(
                row,
                header.ProductIdColumn);

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
                    quantity,
                    sourceLineId,
                    sourceProductId));
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
            int? lineIdColumn = null;
            int? productIdColumn = null;

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

                if (LineIdHeaders.Contains(value, StringComparer.Ordinal))
                {
                    lineIdColumn = columnNumber;
                }

                if (ProductIdHeaders.Contains(value, StringComparer.Ordinal))
                {
                    productIdColumn = columnNumber;
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
                    manufacturerColumn,
                    lineIdColumn,
                    productIdColumn);
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

    private static Guid? ReadOptionalGuid(
        IXLRow row,
        int? columnNumber)
    {
        var value = ReadOptionalCell(row, columnNumber);
        return Guid.TryParse(value, out var result)
            ? result
            : null;
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

    private static ProjectWorkbookMetadata ReadMetadata(
        XLWorkbook workbook,
        Guid calculationId)
    {
        var worksheet = workbook.Worksheets.FirstOrDefault(item =>
            string.Equals(
                item.Name,
                "_Metadata",
                StringComparison.OrdinalIgnoreCase));

        if (worksheet is null
            || !string.Equals(
                worksheet.Cell("B1").GetFormattedString().Trim(),
                "ElectronicCRM.PriceCalculation",
                StringComparison.Ordinal))
        {
            return new ProjectWorkbookMetadata(false, null);
        }

        if (!Guid.TryParse(
                worksheet.Cell("B3").GetFormattedString().Trim(),
                out var workbookCalculationId))
        {
            throw new InvalidDataException(
                "В служебных данных Excel не указан идентификатор проекта.");
        }

        if (workbookCalculationId != calculationId)
        {
            throw new InvalidDataException(
                "Этот Excel-файл выгружен из другого проекта расчёта.");
        }

        return new ProjectWorkbookMetadata(true, null);
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
            null,
            null,
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
            row.SourceLineId,
            null,
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
            row.SourceLineId,
            null,
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
        int? ManufacturerColumn,
        int? LineIdColumn,
        int? ProductIdColumn);

    private sealed record ParsedProjectRow(
        int RowNumber,
        string Article,
        string NormalizedArticle,
        string? SourceName,
        string? SourceManufacturer,
        decimal Quantity,
        Guid? SourceLineId,
        Guid? SourceProductId);

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
        decimal StockQuantity,
        decimal CatalogPriceAmount);

    private sealed record PriceReference(
        Guid ProductId,
        Guid ManufacturerId,
        Guid? PriceListId,
        Guid? PriceListRowId,
        decimal BasePriceAmount,
        decimal? MrcPriceAmount);

    private sealed record ExistingCalculationLine(
        Guid LineId,
        Guid ProductId,
        Guid ManufacturerId,
        Guid? PriceListId,
        Guid? PriceListRowId,
        string Article,
        string Name,
        string ManufacturerName,
        decimal Quantity,
        decimal BasePriceAmount,
        decimal? MrcPriceAmount);

    private sealed record ProjectWorkbookMetadata(
        bool IsProjectWorkbook,
        string? Warning);

    private sealed record ExistingCalculationComponent(
        Guid ComponentLineId,
        Guid MainLineId,
        Guid NeedDefinitionId,
        string NeedName,
        Guid ComponentProductId,
        string Article,
        int QuantityPerUnit);

    private sealed record ComponentNeedReference(
        Guid NeedDefinitionId,
        Guid MainProductTypeId,
        string Name);

    private sealed record ComponentProductReference(
        Guid ProductId,
        Guid ManufacturerId,
        string Article);

    private sealed record ParsedCharacteristicRow(
        int RowNumber,
        Guid? ProductId,
        string Article,
        string CharacteristicCode,
        string Value);

    private sealed record CharacteristicColumnMapping(
        int ColumnNumber,
        string Code);

    private sealed record CharacteristicProductReference(
        Guid ProductId,
        string Article,
        string Name,
        Guid ProductTypeId,
        string ProductTypeName);

    private sealed record CharacteristicDefinitionReference(
        Guid ProductTypeId,
        Guid DefinitionId,
        string Code,
        string Name,
        CharacteristicDataType DataType,
        string? Unit,
        bool IsRequired);

    private sealed record CurrentCharacteristicValue(
        Guid ProductId,
        Guid DefinitionId,
        CharacteristicDataType DataType,
        string? TextValue,
        decimal? NumberValue,
        bool? BooleanValue);

}
