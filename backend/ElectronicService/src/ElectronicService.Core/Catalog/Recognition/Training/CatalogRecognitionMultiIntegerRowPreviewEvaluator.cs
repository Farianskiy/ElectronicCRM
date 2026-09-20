using System.Globalization;

namespace ElectronicService.Core.Catalog.Recognition.Training;

public static class CatalogRecognitionMultiIntegerRowPreviewEvaluator
{
    public static CatalogRecognitionMultiIntegerRowPreviewResult Evaluate(
        Guid manufacturerId,
        Guid productTypeId,
        CatalogRecognitionMultiIntegerPattern pattern,
        CatalogRecognitionMultiIntegerRowPreviewInput row)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(row);
        ArgumentNullException.ThrowIfNull(row.Characteristics);

        if (manufacturerId == Guid.Empty || productTypeId == Guid.Empty)
        {
            return CreateResult(row, "InvalidScope", []);
        }

        if (!CatalogRecognitionMultiIntegerPatternValidator.IsValid(pattern))
        {
            return CreateResult(row, "InvalidPattern", []);
        }

        var characteristicIds = pattern.Parts
            .Where(part => part.CharacteristicDefinitionId.HasValue)
            .Select(part => part.CharacteristicDefinitionId.GetValueOrDefault())
            .OrderBy(id => id)
            .ToArray();

        if (characteristicIds.Length < 2)
        {
            return CreateResult(row, "InvalidPattern", []);
        }

        if (!row.ManufacturerId.HasValue || row.ManufacturerId.Value == Guid.Empty || !row.ProductTypeId.HasValue || row.ProductTypeId.Value == Guid.Empty)
        {
            return CreateWithoutProposals(row, characteristicIds, "ScopeUnknown");
        }

        if (row.ManufacturerId.Value != manufacturerId || row.ProductTypeId.Value != productTypeId)
        {
            return CreateWithoutProposals(row, characteristicIds, "OutsideScope");
        }

        var match = CatalogRecognitionMultiIntegerMatcher.Match(row.ProductName, pattern);

        if (!string.Equals(match.Status, "Matched", StringComparison.Ordinal))
        {
            return CreateWithoutProposals(row, characteristicIds, match.Status);
        }

        var capturesByCharacteristic = match.Captures.ToDictionary(capture => capture.CharacteristicDefinitionId);
        var results = new List<CatalogRecognitionMultiIntegerFieldPreview>(characteristicIds.Length);

        foreach (var characteristicId in characteristicIds)
        {
            row.Characteristics.TryGetValue(characteristicId.ToString(), out var currentValue);
            var capture = capturesByCharacteristic[characteristicId];
            var status = CompareValues(currentValue, capture.NormalizedValue);

            results.Add(new CatalogRecognitionMultiIntegerFieldPreview(
                characteristicId,
                status,
                currentValue,
                capture.NormalizedValue,
                capture));
        }

        return CreateResult(row, "Matched", results);
    }

    private static string CompareValues(string? currentValue, string proposedValue)
    {
        if (!decimal.TryParse(proposedValue, NumberStyles.None, CultureInfo.InvariantCulture, out var proposedNumber))
        {
            return "NumberOutOfRange";
        }

        if (string.IsNullOrWhiteSpace(currentValue))
        {
            return "SuggestedValue";
        }

        if (!decimal.TryParse(currentValue, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var currentNumber))
        {
            return "CurrentValueNotComparable";
        }

        if (currentNumber == proposedNumber)
        {
            return "MatchesCurrentValue";
        }

        return "DiffersFromCurrentValue";
    }

    private static CatalogRecognitionMultiIntegerRowPreviewResult CreateWithoutProposals(
        CatalogRecognitionMultiIntegerRowPreviewInput row,
        Guid[] characteristicIds,
        string status)
    {
        var results = new List<CatalogRecognitionMultiIntegerFieldPreview>(characteristicIds.Length);

        foreach (var characteristicId in characteristicIds)
        {
            row.Characteristics.TryGetValue(characteristicId.ToString(), out var currentValue);

            results.Add(new CatalogRecognitionMultiIntegerFieldPreview(
                characteristicId,
                status,
                currentValue,
                null,
                null));
        }

        return CreateResult(row, status, results);
    }

    private static CatalogRecognitionMultiIntegerRowPreviewResult CreateResult(
        CatalogRecognitionMultiIntegerRowPreviewInput row,
        string status,
        IReadOnlyList<CatalogRecognitionMultiIntegerFieldPreview> fields)
    {
        return new CatalogRecognitionMultiIntegerRowPreviewResult(
            row.RowId,
            row.RowNumber,
            row.ProductName,
            status,
            fields);
    }
}