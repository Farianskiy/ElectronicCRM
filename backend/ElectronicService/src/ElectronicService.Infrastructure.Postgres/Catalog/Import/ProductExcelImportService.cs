using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ClosedXML.Excel;
using ElectronicService.Core.Catalog.Import.ImportProductsFromExcel;
using ElectronicService.Core.Catalog.Import.PreviewProductsExcelImport;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.Manufacturers;
using ElectronicService.Domain.Catalog.Products;
using ElectronicService.Domain.Catalog.ProductTypes;
using ElectronicService.Domain.Catalog.ValueObjects;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Import;

public sealed class ProductExcelImportService : IProductsExcelImporter
{
    private static readonly Action<ILogger, string, int, int, int, Exception?> LogImportCompleted =
        LoggerMessage.Define<string, int, int, int>(
            LogLevel.Information,
            new EventId(1, nameof(LogImportCompleted)),
            "Excel import completed. File: {FileName}. Total rows: {TotalRows}. Imported: {ImportedRows}. Skipped: {SkippedRows}.");

    private readonly ElectronicDbContext _dbContext;
    private readonly ILogger<ProductExcelImportService> _logger;
    
    private const int PreviewRowsLimit = 100;

    public ProductExcelImportService(
        ElectronicDbContext dbContext,
        ILogger<ProductExcelImportService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ImportProductsFromExcelResult> ImportAsync(
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var profile = ExcelImportProfileResolver.Resolve(fileName);

        var errors = new List<string>();

        var productType = await _dbContext.ProductTypes
            .Include(type => type.Characteristics)
            .FirstOrDefaultAsync(
                type => type.Code == profile.ProductTypeCode,
                cancellationToken)
            .ConfigureAwait(false);

        if (productType is null)
        {
            throw new InvalidOperationException(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Product type '{profile.ProductTypeCode}' was not found."));
        }

        var characteristicDefinitions = await _dbContext.CharacteristicDefinitions
            .ToDictionaryAsync(
                characteristic => characteristic.Code,
                StringComparer.Ordinal,
                cancellationToken)
            .ConfigureAwait(false);

        var manufacturersByNormalizedName = await _dbContext.Manufacturers
            .ToDictionaryAsync(
                manufacturer => manufacturer.NormalizedName,
                StringComparer.Ordinal,
                cancellationToken)
            .ConfigureAwait(false);

        var existingArticlesList = await _dbContext.Products
            .Select(product => product.Article.Value)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var knownArticles = existingArticlesList.ToHashSet(StringComparer.Ordinal);

        using var workbook = new XLWorkbook(fileStream);

        var worksheet = workbook.Worksheets.First();

        var headerMap = BuildHeaderMap(worksheet);

        var lastRowNumber = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        var totalRows = 0;
        var importedRows = 0;
        var skippedRows = 0;

        for (var rowNumber = 2; rowNumber <= lastRowNumber; rowNumber++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            totalRows++;

            var row = worksheet.Row(rowNumber);

            try
            {
                var name = GetRequiredCellValue(row, headerMap, profile.NameColumn);

                if (string.IsNullOrWhiteSpace(name))
                {
                    skippedRows++;
                    continue;
                }

                var rawManufacturerName = GetRequiredCellValue(row, headerMap, profile.ManufacturerColumn);

                var manufacturerNormalization = ManufacturerImportNormalizer.NormalizeManufacturerName(
                    rawManufacturerName);

                var manufacturer = GetOrCreateManufacturer(
                    manufacturerNormalization.NormalizedName,
                    manufacturersByNormalizedName);

                var article = GetArticle(row, headerMap, profile, name);

                if (knownArticles.Contains(article))
                {
                    skippedRows++;
                    continue;
                }

                var price = Money.Zero();

                var stockQuantity = StockQuantity.Zero();

                var productResult = Product.Create(
                    article,
                    name,
                    productType.Id,
                    manufacturer.Id,
                    price,
                    stockQuantity);

                if (productResult.IsFailure)
                {
                    errors.Add(
                        string.Create(
                            CultureInfo.InvariantCulture,
                            $"Row {rowNumber}: {productResult.Error.Message}"));

                    skippedRows++;
                    continue;
                }

                var product = productResult.Value;

                AddDefaultCharacteristics(
                    product,
                    productType,
                    characteristicDefinitions,
                    profile);

                AddCharacteristicsFromRow(
                    product,
                    productType,
                    characteristicDefinitions,
                    row,
                    headerMap,
                    profile);

                var missingRequiredCharacteristicError = CreateMissingRequiredCharacteristicError(
                    product,
                    productType,
                    characteristicDefinitions);

                if (missingRequiredCharacteristicError is not null)
                {
                    errors.Add(
                        string.Create(
                            CultureInfo.InvariantCulture,
                            $"Row {rowNumber}: {missingRequiredCharacteristicError}"));

                    skippedRows++;
                    continue;
                }

                var requiredValidationResult = product.ValidateRequiredCharacteristics(productType);

                if (requiredValidationResult.IsFailure)
                {
                    errors.Add(
                        string.Create(
                            CultureInfo.InvariantCulture,
                            $"Row {rowNumber}: {requiredValidationResult.Error.Message}"));

                    skippedRows++;
                    continue;
                }

                await _dbContext.Products
                    .AddAsync(product, cancellationToken)
                    .ConfigureAwait(false);

                knownArticles.Add(article);
                importedRows++;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                errors.Add(
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"Row {rowNumber}: {exception.Message}"));

                skippedRows++;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        LogImportCompleted(
            _logger,
            fileName,
            totalRows,
            importedRows,
            skippedRows,
            null);

        return new ImportProductsFromExcelResult(
            totalRows,
            importedRows,
            skippedRows,
            errors);
    }

    public async Task<PreviewProductsExcelImportResult> PreviewAsync(
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var profile = ExcelImportProfileResolver.Resolve(fileName);

        var errors = new List<string>();

        var productType = await _dbContext.ProductTypes
            .Include(type => type.Characteristics)
            .FirstOrDefaultAsync(
                type => type.Code == profile.ProductTypeCode,
                cancellationToken)
            .ConfigureAwait(false);

        if (productType is null)
        {
            throw new InvalidOperationException(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Product type '{profile.ProductTypeCode}' was not found."));
        }

        var characteristicDefinitions = await _dbContext.CharacteristicDefinitions
            .ToDictionaryAsync(
                characteristic => characteristic.Code,
                StringComparer.Ordinal,
                cancellationToken)
            .ConfigureAwait(false);

        var manufacturersByNormalizedName = await _dbContext.Manufacturers
            .ToDictionaryAsync(
                manufacturer => manufacturer.NormalizedName,
                StringComparer.Ordinal,
                cancellationToken)
            .ConfigureAwait(false);

        var existingArticlesList = await _dbContext.Products
            .Select(product => product.Article.Value)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var existingArticles = existingArticlesList.ToHashSet(StringComparer.Ordinal);

        using var workbook = new XLWorkbook(fileStream);

        var worksheet = workbook.Worksheets.First();

        var headerMap = BuildHeaderMap(worksheet);

        var lastRowNumber = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        var totalRows = 0;
        var createRows = 0;
        var duplicateRows = 0;
        var errorRows = 0;
        var normalizedManufacturerRows = 0;
        var newManufacturerRows = 0;

        var previewRows = new List<PreviewProductsExcelImportRow>();
        var manufacturerNormalizations = new Dictionary<string, int>(StringComparer.Ordinal);

        for (var rowNumber = 2; rowNumber <= lastRowNumber; rowNumber++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            totalRows++;

            var row = worksheet.Row(rowNumber);

            var rowErrors = new List<string>();
            var warnings = new List<string>();
            var characteristics = new Dictionary<string, string>(StringComparer.Ordinal);

            string? name = null;
            string? article = null;
            var rawManufacturerName = string.Empty;
            var normalizedManufacturerName = string.Empty;
            var manufacturerAction = "Unknown";
            var action = "Error";

            try
            {
                name = GetRequiredCellValue(row, headerMap, profile.NameColumn);

                if (string.IsNullOrWhiteSpace(name))
                {
                    rowErrors.Add("Наименование не заполнено.");
                }

                rawManufacturerName = GetRequiredCellValue(row, headerMap, profile.ManufacturerColumn);

                var manufacturerNormalization = ManufacturerImportNormalizer.NormalizeManufacturerName(
                    rawManufacturerName);

                normalizedManufacturerName = manufacturerNormalization.NormalizedName;

                if (manufacturerNormalization.WasChanged)
                {
                    normalizedManufacturerRows++;

                    warnings.Add(
                        string.Create(
                            CultureInfo.InvariantCulture,
                            $"Производитель будет нормализован: '{manufacturerNormalization.RawName}' -> '{manufacturerNormalization.NormalizedName}'."));

                    var manufacturerNormalizationKey = string.Create(
                        CultureInfo.InvariantCulture,
                        $"{manufacturerNormalization.RawName}|{manufacturerNormalization.NormalizedName}");

                    if (!manufacturerNormalizations.TryAdd(manufacturerNormalizationKey, 1))
                    {
                        manufacturerNormalizations[manufacturerNormalizationKey]++;
                    }
                }

                var normalizedManufacturerLookupKey = NormalizeText(normalizedManufacturerName);

                if (manufacturersByNormalizedName.ContainsKey(normalizedManufacturerLookupKey))
                {
                    manufacturerAction = "UseExisting";
                }
                else
                {
                    manufacturerAction = "CreateNew";
                    newManufacturerRows++;

                    warnings.Add(
                        string.Create(
                            CultureInfo.InvariantCulture,
                            $"Будет создан новый производитель: '{normalizedManufacturerName}'."));
                }

                if (!string.IsNullOrWhiteSpace(name))
                {
                    article = GetArticle(row, headerMap, profile, name);

                    if (existingArticles.Contains(article))
                    {
                        action = "Duplicate";
                        duplicateRows++;

                        warnings.Add(
                            string.Create(
                                CultureInfo.InvariantCulture,
                                $"Товар с артикулом '{article}' уже существует и будет пропущен."));
                    }
                }

                AddPreviewDefaultCharacteristics(
                    characteristics,
                    productType,
                    characteristicDefinitions,
                    profile,
                    rowErrors);

                AddPreviewCharacteristicsFromRow(
                    characteristics,
                    productType,
                    characteristicDefinitions,
                    row,
                    headerMap,
                    profile,
                    rowErrors);

                var existingCharacteristicDefinitionIds = characteristics.Keys
                    .Where(characteristicDefinitions.ContainsKey)
                    .Select(characteristicCode => characteristicDefinitions[characteristicCode].Id)
                    .ToHashSet();

                var missingRequiredCharacteristicId = productType
                    .FindMissingRequiredCharacteristicId(existingCharacteristicDefinitionIds);

                if (missingRequiredCharacteristicId is not null)
                {
                    var missingCharacteristicDefinition = characteristicDefinitions.Values
                        .FirstOrDefault(characteristic =>
                            characteristic.Id == missingRequiredCharacteristicId.Value);

                    if (missingCharacteristicDefinition is null)
                    {
                        rowErrors.Add(
                            string.Create(
                                CultureInfo.InvariantCulture,
                                $"Обязательная характеристика '{missingRequiredCharacteristicId.Value}' не заполнена."));
                    }
                    else
                    {
                        rowErrors.Add(
                            string.Create(
                                CultureInfo.InvariantCulture,
                                $"Обязательная характеристика '{missingCharacteristicDefinition.Name}' ({missingCharacteristicDefinition.Code}) не заполнена."));
                    }
                }

                if (rowErrors.Count > 0)
                {
                    action = "Error";
                    errorRows++;
                }
                else if (!string.Equals(action, "Duplicate", StringComparison.Ordinal))
                {
                    action = "Create";
                    createRows++;

                    if (article is not null)
                    {
                        existingArticles.Add(article);
                    }
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                rowErrors.Add(exception.Message);
                action = "Error";
                errorRows++;
            }

            if (previewRows.Count < PreviewRowsLimit)
            {
                previewRows.Add(new PreviewProductsExcelImportRow(
                    rowNumber,
                    action,
                    article,
                    name,
                    profile.ProductTypeCode,
                    rawManufacturerName,
                    normalizedManufacturerName,
                    manufacturerAction,
                    characteristics,
                    warnings,
                    rowErrors));
            }
        }

        var manufacturerNormalizationResults = manufacturerNormalizations
            .Select(item =>
            {
                var parts = item.Key.Split('|', 2);

                return new PreviewProductsExcelImportManufacturerNormalization(
                    parts[0],
                    parts.Length > 1 ? parts[1] : string.Empty,
                    item.Value);
            })
            .OrderByDescending(item => item.RowsCount)
            .ThenBy(item => item.RawName, StringComparer.Ordinal)
            .ToList();

        if (totalRows > PreviewRowsLimit)
        {
            errors.Add(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Preview response contains first {PreviewRowsLimit} rows. Total rows: {totalRows}."));
        }

        return new PreviewProductsExcelImportResult(
            fileName,
            profile.ProductTypeCode,
            productType.Name,
            totalRows,
            createRows,
            duplicateRows,
            errorRows,
            normalizedManufacturerRows,
            newManufacturerRows,
            PreviewRowsLimit,
            totalRows > PreviewRowsLimit,
            manufacturerNormalizationResults,
            previewRows,
            errors);
    }

    private Manufacturer GetOrCreateManufacturer(
    string manufacturerName,
    Dictionary<string, Manufacturer> manufacturersByNormalizedName)
    {
        var normalizedName = NormalizeText(manufacturerName);

        if (manufacturersByNormalizedName.TryGetValue(normalizedName, out var existingManufacturer))
        {
            return existingManufacturer;
        }

        var manufacturerResult = Manufacturer.Create(manufacturerName);

        if (manufacturerResult.IsFailure)
        {
            throw new InvalidOperationException(manufacturerResult.Error.Message);
        }

        var manufacturer = manufacturerResult.Value;

        if (manufacturersByNormalizedName.TryGetValue(
                manufacturer.NormalizedName,
                out var existingManufacturerAfterCreate))
        {
            return existingManufacturerAfterCreate;
        }

        manufacturersByNormalizedName.Add(manufacturer.NormalizedName, manufacturer);

        _dbContext.Manufacturers.Add(manufacturer);

        return manufacturer;
    }

    private static Dictionary<string, int> BuildHeaderMap(IXLWorksheet worksheet)
    {
        var headerRow = worksheet.Row(1);

        var lastColumnNumber = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;

        var result = new Dictionary<string, int>(StringComparer.Ordinal);

        for (var columnNumber = 1; columnNumber <= lastColumnNumber; columnNumber++)
        {
            var header = headerRow.Cell(columnNumber).GetString().Trim();

            if (string.IsNullOrWhiteSpace(header))
            {
                continue;
            }

            if (!result.ContainsKey(header))
            {
                result.TryAdd(header, columnNumber);
            }
        }

        return result;
    }

    private static string GetRequiredCellValue(
        IXLRow row,
        Dictionary<string, int> headerMap,
        string columnName)
    {
        if (!headerMap.TryGetValue(columnName, out var columnNumber))
        {
            throw new InvalidOperationException(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Column '{columnName}' was not found."));
        }

        return row.Cell(columnNumber).GetFormattedString().Trim();
    }

    private static string? GetOptionalCellValue(
        IXLRow row,
        Dictionary<string, int> headerMap,
        string? columnName)
    {
        if (string.IsNullOrWhiteSpace(columnName))
        {
            return null;
        }

        if (!headerMap.TryGetValue(columnName, out var columnNumber))
        {
            return null;
        }

        var value = row.Cell(columnNumber).GetFormattedString().Trim();

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string GetArticle(
        IXLRow row,
        Dictionary<string, int> headerMap,
        ExcelImportProfile profile,
        string productName)
    {
        var article = GetOptionalCellValue(row, headerMap, profile.ArticleColumn);

        if (!string.IsNullOrWhiteSpace(article))
        {
            return article;
        }

        var code = GetOptionalCellValue(row, headerMap, profile.CodeColumn);

        if (!string.IsNullOrWhiteSpace(code))
        {
            return code;
        }

        return GenerateTechnicalArticle(profile.ProductTypeCode, productName);
    }

    private static string GenerateTechnicalArticle(string productTypeCode, string productName)
    {
        var source = string.Create(
            CultureInfo.InvariantCulture,
            $"{productTypeCode}|{NormalizeText(productName)}");

        var bytes = Encoding.UTF8.GetBytes(source);

        var hash = SHA256.HashData(bytes);

        var hashText = Convert.ToHexString(hash);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"AUTO-{hashText[..12]}");
    }

    private static void AddDefaultCharacteristics(
        Product product,
        ProductType productType,
        IReadOnlyDictionary<string, CharacteristicDefinition> characteristicDefinitions,
        ExcelImportProfile profile)
    {
        if (!string.IsNullOrWhiteSpace(profile.DefaultCabinetKind))
        {
            SetTextCharacteristic(
                product,
                productType,
                characteristicDefinitions,
                "CABINET_KIND",
                profile.DefaultCabinetKind);
        }

        if (!string.IsNullOrWhiteSpace(profile.DefaultProductSeries))
        {
            SetTextCharacteristic(
                product,
                productType,
                characteristicDefinitions,
                "PRODUCT_SERIES",
                profile.DefaultProductSeries);
        }
    }

    private static void AddCharacteristicsFromRow(
        Product product,
        ProductType productType,
        Dictionary<string, CharacteristicDefinition> characteristicDefinitions,
        IXLRow row,
        Dictionary<string, int> headerMap,
        ExcelImportProfile profile)
    {
        foreach (var columnMapItem in profile.CharacteristicColumnMap)
        {
            var excelColumnName = columnMapItem.Key;
            var characteristicCode = columnMapItem.Value;

            var rawValue = GetOptionalCellValue(row, headerMap, excelColumnName);

            if (string.IsNullOrWhiteSpace(rawValue))
            {
                continue;
            }

            SetCharacteristic(
                product,
                productType,
                characteristicDefinitions,
                characteristicCode,
                rawValue);
        }

        if (string.Equals(
                profile.ProductTypeCode,
                "MODULAR_DISTRIBUTION_CABINET",
                StringComparison.Ordinal)
            && !product.Characteristics.Any(characteristic =>
                characteristic.CharacteristicDefinitionId == characteristicDefinitions["CABINET_KIND"].Id))
        {
            var mountingMethod = GetOptionalCellValue(row, headerMap, "Способ установки");

            var cabinetKind = ResolveModularDistributionCabinetKind(mountingMethod);

            SetTextCharacteristic(
                product,
                productType,
                characteristicDefinitions,
                "CABINET_KIND",
                cabinetKind);
        }
    }

    private static void AddPreviewDefaultCharacteristics(
    Dictionary<string, string> characteristics,
    ProductType productType,
    IReadOnlyDictionary<string, CharacteristicDefinition> characteristicDefinitions,
    ExcelImportProfile profile,
    List<string> errors)
{
    if (!string.IsNullOrWhiteSpace(profile.DefaultCabinetKind))
    {
        AddPreviewCharacteristic(
            characteristics,
            productType,
            characteristicDefinitions,
            "CABINET_KIND",
            profile.DefaultCabinetKind,
            errors);
    }

    if (!string.IsNullOrWhiteSpace(profile.DefaultProductSeries))
    {
        AddPreviewCharacteristic(
            characteristics,
            productType,
            characteristicDefinitions,
            "PRODUCT_SERIES",
            profile.DefaultProductSeries,
            errors);
    }
}

    private static void AddPreviewCharacteristicsFromRow(
        Dictionary<string, string> characteristics,
        ProductType productType,
        Dictionary<string, CharacteristicDefinition> characteristicDefinitions,
        IXLRow row,
        Dictionary<string, int> headerMap,
        ExcelImportProfile profile,
        List<string> errors)
    {
        foreach (var columnMapItem in profile.CharacteristicColumnMap)
        {
            var excelColumnName = columnMapItem.Key;
            var characteristicCode = columnMapItem.Value;

            var rawValue = GetOptionalCellValue(row, headerMap, excelColumnName);

            if (string.IsNullOrWhiteSpace(rawValue))
            {
                continue;
            }

            AddPreviewCharacteristic(
                characteristics,
                productType,
                characteristicDefinitions,
                characteristicCode,
                rawValue,
                errors);
        }

        if (string.Equals(
                profile.ProductTypeCode,
                "MODULAR_DISTRIBUTION_CABINET",
                StringComparison.Ordinal)
            && !characteristics.ContainsKey("CABINET_KIND"))
        {
            var mountingMethod = GetOptionalCellValue(row, headerMap, "Способ установки");

            var cabinetKind = ResolveModularDistributionCabinetKind(mountingMethod);

            AddPreviewCharacteristic(
                characteristics,
                productType,
                characteristicDefinitions,
                "CABINET_KIND",
                cabinetKind,
                errors);
        }
    }

    private static void AddPreviewCharacteristic(
        Dictionary<string, string> characteristics,
        ProductType productType,
        IReadOnlyDictionary<string, CharacteristicDefinition> characteristicDefinitions,
        string characteristicCode,
        string rawValue,
        List<string> errors)
    {
        if (!characteristicDefinitions.TryGetValue(characteristicCode, out var definition))
        {
            errors.Add(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Characteristic '{characteristicCode}' was not found."));

            return;
        }

        if (!productType.AllowsCharacteristic(definition.Id))
        {
            errors.Add(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Characteristic '{characteristicCode}' is not allowed for product type '{productType.Code}'."));

            return;
        }

        try
        {
            var valueResult = CreateCharacteristicValue(characteristicCode, definition, rawValue);

            if (valueResult.IsFailure)
            {
                errors.Add(valueResult.Error.Message);
                return;
            }

            characteristics[characteristicCode] = rawValue;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            errors.Add(exception.Message);
        }
    }

    private static void SetCharacteristic(
        Product product,
        ProductType productType,
        Dictionary<string, CharacteristicDefinition> characteristicDefinitions,
        string characteristicCode,
        string rawValue)
    {
        if (!characteristicDefinitions.TryGetValue(characteristicCode, out var definition))
        {
            throw new InvalidOperationException(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Characteristic '{characteristicCode}' was not found."));
        }

        var valueResult = CreateCharacteristicValue(characteristicCode, definition, rawValue);

        if (valueResult.IsFailure)
        {
            throw new InvalidOperationException(valueResult.Error.Message);
        }

        var setResult = product.SetCharacteristic(
            productType,
            definition,
            valueResult.Value);

        if (setResult.IsFailure)
        {
            throw new InvalidOperationException(setResult.Error.Message);
        }
    }

    private static void SetTextCharacteristic(
        Product product,
        ProductType productType,
        IReadOnlyDictionary<string, CharacteristicDefinition> characteristicDefinitions,
        string characteristicCode,
        string value)
    {
        if (!characteristicDefinitions.TryGetValue(characteristicCode, out var definition))
        {
            throw new InvalidOperationException(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Characteristic '{characteristicCode}' was not found."));
        }

        var valueResult = CharacteristicValue.CreateText(value);

        if (valueResult.IsFailure)
        {
            throw new InvalidOperationException(valueResult.Error.Message);
        }

        var setResult = product.SetCharacteristic(
            productType,
            definition,
            valueResult.Value);

        if (setResult.IsFailure)
        {
            throw new InvalidOperationException(setResult.Error.Message);
        }
    }

    private static CSharpFunctionalExtensions.Result<CharacteristicValue, Domain.Common.DomainError> CreateCharacteristicValue(
        string characteristicCode,
        CharacteristicDefinition definition,
        string rawValue)
    {
        return definition.DataType switch
        {
            CharacteristicDataType.Text => CharacteristicValue.CreateText(rawValue),

            CharacteristicDataType.Number => CharacteristicValue.CreateNumber(
                ParseNumber(characteristicCode, rawValue)),

            CharacteristicDataType.Boolean => CharacteristicValue.CreateBoolean(
                ParseBoolean(rawValue)),

            _ => CSharpFunctionalExtensions.Result.Failure<CharacteristicValue, Domain.Common.DomainError>(
                Domain.Common.GeneralErrors.ValueIsInvalid(nameof(definition.DataType)))
        };
    }

    private static decimal ParseNumber(string characteristicCode, string rawValue)
    {
        var normalizedValue = rawValue
            .Trim()
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace(",", ".", StringComparison.Ordinal);

        var numberText = new string(
            normalizedValue
                .Where(character => char.IsDigit(character) || character == '.' || character == '-')
                .ToArray());

        if (!decimal.TryParse(
                numberText,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var value))
        {
            throw new InvalidOperationException(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Cannot parse number from value '{rawValue}'."));
        }

        if (string.Equals(characteristicCode, "BREAKING_CAPACITY", StringComparison.Ordinal)
            && value >= 1000)
        {
            return value / 1000;
        }

        return value;
    }

    private static bool ParseBoolean(string rawValue)
    {
        var normalizedValue = NormalizeText(rawValue);

        if (string.Equals(normalizedValue, "TRUE", StringComparison.Ordinal)
            || string.Equals(normalizedValue, "ДА", StringComparison.Ordinal)
            || string.Equals(normalizedValue, "ЕСТЬ", StringComparison.Ordinal)
            || string.Equals(normalizedValue, "1", StringComparison.Ordinal)
            || string.Equals(normalizedValue, "+", StringComparison.Ordinal)
            || string.Equals(normalizedValue, "РЕВЕРСИВНЫЙ", StringComparison.Ordinal))
        {
            return true;
        }

        if (string.Equals(normalizedValue, "FALSE", StringComparison.Ordinal)
            || string.Equals(normalizedValue, "НЕТ", StringComparison.Ordinal)
            || string.Equals(normalizedValue, "ОТСУТСТВУЕТ", StringComparison.Ordinal)
            || string.Equals(normalizedValue, "0", StringComparison.Ordinal)
            || string.Equals(normalizedValue, "-", StringComparison.Ordinal)
            || string.Equals(normalizedValue, "НЕРЕВЕРСИВНЫЙ", StringComparison.Ordinal))
        {
            return false;
        }

        return !normalizedValue.Contains("НЕТ", StringComparison.Ordinal);
    }

    private static string ResolveModularDistributionCabinetKind(string? mountingMethod)
    {
        var normalizedMountingMethod = NormalizeText(mountingMethod ?? string.Empty);

        if (normalizedMountingMethod.Contains("ВСТРА", StringComparison.Ordinal))
        {
            return "ЩРв";
        }

        return "ЩРн";
    }

    private static string NormalizeText(string value)
    {
        return value
            .Trim()
            .ToUpperInvariant()
            .Replace("Ё", "Е", StringComparison.Ordinal);
    }

    private static string? CreateMissingRequiredCharacteristicError(
    Product product,
    ProductType productType,
    Dictionary<string, CharacteristicDefinition> characteristicDefinitions)
    {
        var existingCharacteristicDefinitionIds = product.Characteristics
            .Select(characteristic => characteristic.CharacteristicDefinitionId)
            .ToHashSet();

        var missingRequiredCharacteristicId = productType
            .FindMissingRequiredCharacteristicId(existingCharacteristicDefinitionIds);

        if (missingRequiredCharacteristicId is null)
        {
            return null;
        }

        var missingCharacteristicDefinition = characteristicDefinitions.Values
            .FirstOrDefault(characteristic =>
                characteristic.Id == missingRequiredCharacteristicId.Value);

        if (missingCharacteristicDefinition is null)
        {
            return string.Create(
                CultureInfo.InvariantCulture,
                $"Обязательная характеристика '{missingRequiredCharacteristicId.Value}' не заполнена.");
        }

        return string.Create(
            CultureInfo.InvariantCulture,
            $"Обязательная характеристика '{missingCharacteristicDefinition.Name}' ({missingCharacteristicDefinition.Code}) не заполнена.");
    }
}