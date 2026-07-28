using System.Globalization;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Domain.Catalog.ProductTypes;
using ElectronicService.Domain.Catalog.ValueObjects;

namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

public sealed class CatalogImportRowValidator : ICatalogImportRowValidator
{
    public CatalogImportRowValidationResult Validate(
        CatalogImportNormalizedRowData data,
        ProductType productType,
        IReadOnlyCollection<CharacteristicDefinition> characteristicDefinitions)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(productType);
        ArgumentNullException.ThrowIfNull(characteristicDefinitions);

        var issues = new List<CatalogImportRowIssue>();
        var warnings = new List<CatalogImportRowIssue>();

        var normalizedName = ValidateName(data.Name, issues);
        var normalizedArticle = ValidateArticle(data.Article, issues);

        var normalizedManufacturer = string.IsNullOrWhiteSpace(data.Manufacturer)
            ? null
            : data.Manufacturer.Trim();

        if (data.ManufacturerId is null || data.ManufacturerId == Guid.Empty)
        {
            issues.Add(
                CreateIssue(
                    "manufacturer.required",
                    "Необходимо выбрать производителя.",
                    "manufacturerId"));
        }

        if (string.IsNullOrWhiteSpace(normalizedManufacturer))
        {
            issues.Add(
                CreateIssue(
                    "manufacturer.required",
                    "Не указано название производителя.",
                    "manufacturer"));
        }

        if (data.Price is < 0)
        {
            issues.Add(
                CreateIssue(
                    "price.invalid",
                    "Цена должна быть неотрицательным числом.",
                    "price"));
        }

        if (data.StockQuantity is < 0)
        {
            issues.Add(
                CreateIssue(
                    "stock.invalid",
                    "Остаток должен быть неотрицательным целым числом.",
                    "stockQuantity"));
        }

        var normalizedCharacteristics = ValidateCharacteristics(
            data.Characteristics,
            productType,
            characteristicDefinitions,
            issues);

        var status = issues.Count == 0
            ? CatalogImportRowStatus.Valid
            : CatalogImportRowStatus.Error;

        var normalizedData = new CatalogImportNormalizedRowData(
            normalizedName,
            normalizedArticle,
            normalizedManufacturer,
            data.Price,
            data.StockQuantity,
            normalizedCharacteristics,
            data.ManufacturerId);

        return new CatalogImportRowValidationResult(
            status,
            normalizedData,
            issues,
            warnings);
    }

    private static string? ValidateName(
        string? name,
        List<CatalogImportRowIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            issues.Add(
                CreateIssue(
                    "name.required",
                    "Не указано наименование товара.",
                    "name"));

            return null;
        }

        var result = ProductName.Create(name);

        if (result.IsFailure)
        {
            issues.Add(
                CreateIssue(
                    "name.invalid",
                    result.Error.Message,
                    "name"));

            return name.Trim();
        }

        return result.Value.Value;
    }

    private static string? ValidateArticle(
        string? article,
        List<CatalogImportRowIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(article))
        {
            issues.Add(
                CreateIssue(
                    "article.required",
                    "Не указан артикул товара.",
                    "article"));

            return null;
        }

        var result = ProductArticle.Create(article);

        if (result.IsFailure)
        {
            issues.Add(
                CreateIssue(
                    "article.invalid",
                    result.Error.Message,
                    "article"));

            return article.Trim();
        }

        return result.Value.Value;
    }

    private static Dictionary<string, string> ValidateCharacteristics(
        IReadOnlyDictionary<string, string> characteristics,
        ProductType productType,
        IReadOnlyCollection<CharacteristicDefinition> characteristicDefinitions,
        List<CatalogImportRowIssue> issues)
    {
        var definitionsById = characteristicDefinitions.ToDictionary(
            definition => definition.Id);

        var normalizedCharacteristics = new Dictionary<string, string>(
            StringComparer.Ordinal);

        foreach (var characteristic in characteristics)
        {
            if (!Guid.TryParse(characteristic.Key, out var definitionId)
                || definitionId == Guid.Empty)
            {
                issues.Add(
                    CreateIssue(
                        "characteristic.invalid_id",
                        $"Идентификатор характеристики '{characteristic.Key}' некорректен.",
                        characteristic.Key));

                continue;
            }

            if (!productType.AllowsCharacteristic(definitionId))
            {
                issues.Add(
                    CreateIssue(
                        "characteristic.not_allowed",
                        $"Характеристика '{definitionId}' не разрешена для выбранного типа товара.",
                        characteristic.Key));

                continue;
            }

            if (!definitionsById.TryGetValue(definitionId, out var definition))
            {
                issues.Add(
                    CreateIssue(
                        "characteristic.not_found",
                        $"Определение характеристики '{definitionId}' не найдено.",
                        characteristic.Key));

                continue;
            }

            if (string.IsNullOrWhiteSpace(characteristic.Value))
            {
                continue;
            }

            if (!TryNormalizeCharacteristicValue(
                characteristic.Value,
                definition,
                out var normalizedValue))
            {
                issues.Add(
                    CreateIssue(
                        "characteristic.invalid",
                        $"Значение характеристики '{definition.Name}' не соответствует типу '{definition.DataType}'.",
                        definitionId.ToString()));

                continue;
            }

            normalizedCharacteristics[definitionId.ToString()] = normalizedValue;
        }

        foreach (var productTypeCharacteristic in productType.Characteristics)
        {
            if (!productTypeCharacteristic.IsRequired)
            {
                continue;
            }

            var definitionId = productTypeCharacteristic.CharacteristicDefinitionId;
            var definitionKey = definitionId.ToString();

            if (normalizedCharacteristics.ContainsKey(definitionKey))
            {
                continue;
            }

            var displayName = definitionsById.TryGetValue(
                definitionId,
                out var definition)
                ? definition.Name
                : definitionKey;

            issues.Add(
                CreateIssue(
                    "characteristic.required",
                    $"Не заполнена обязательная характеристика '{displayName}'.",
                    definitionKey));
        }

        return normalizedCharacteristics;
    }

    private static bool TryNormalizeCharacteristicValue(
        string rawValue,
        CharacteristicDefinition definition,
        out string normalizedValue)
    {
        switch (definition.DataType)
        {
            case CharacteristicDataType.Text:
                normalizedValue = rawValue.Trim();
                return normalizedValue.Length > 0;

            case CharacteristicDataType.Number:
                var numberSource = rawValue
                    .Trim()
                    .Replace(",", ".", StringComparison.Ordinal);

                if (decimal.TryParse(
                    numberSource,
                    NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture,
                    out var number))
                {
                    normalizedValue = number.ToString(CultureInfo.InvariantCulture);
                    return true;
                }

                break;

            case CharacteristicDataType.Boolean:
                if (TryParseBoolean(rawValue, out var boolean))
                {
                    normalizedValue = boolean ? "true" : "false";
                    return true;
                }

                break;
        }

        normalizedValue = string.Empty;
        return false;
    }

    private static bool TryParseBoolean(string rawValue, out bool value)
    {
        var normalized = rawValue
            .Trim()
            .ToUpperInvariant()
            .Replace("Ё", "Е", StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal);

        switch (normalized)
        {
            case "ДА":
            case "YES":
            case "TRUE":
            case "1":
            case "+":
            case "ЕСТЬ":
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

    private static CatalogImportRowIssue CreateIssue(
        string code,
        string message,
        string? field)
    {
        return new CatalogImportRowIssue(
            code,
            message,
            field,
            SourceColumnNumber: null);
    }
}