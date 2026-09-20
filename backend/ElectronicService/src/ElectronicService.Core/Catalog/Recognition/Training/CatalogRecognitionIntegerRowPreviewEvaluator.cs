using System.Globalization;

namespace ElectronicService.Core.Catalog.Recognition.Training;

public static class CatalogRecognitionIntegerRowPreviewEvaluator
{
    public static CatalogRecognitionIntegerRowPreviewResult Evaluate(
        CatalogRecognitionTrainingScope scope,
        CatalogRecognitionIntegerAlternativesPattern pattern,
        CatalogRecognitionLiteralRowPreviewInput row)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(row);

        if (scope.ManufacturerId == Guid.Empty || scope.ProductTypeId == Guid.Empty || scope.CharacteristicDefinitionId == Guid.Empty)
        {
            return CreateResult(row, "InvalidScope", null, Array.Empty<CatalogRecognitionIntegerCapture>());
        }

        if (pattern.Prefix is null || pattern.Suffixes is null || pattern.Prefix.Length > 2000 || pattern.Suffixes.Count == 0 || pattern.Suffixes.Count > 16)
        {
            return CreateResult(row, "InvalidPattern", null, Array.Empty<CatalogRecognitionIntegerCapture>());
        }

        if (pattern.Suffixes.Any(suffix => suffix is null || suffix.Length > 2000 || (pattern.Prefix.Length == 0 && suffix.Length == 0)))
        {
            return CreateResult(row, "InvalidPattern", null, Array.Empty<CatalogRecognitionIntegerCapture>());
        }

        if (!row.ManufacturerId.HasValue || row.ManufacturerId.Value == Guid.Empty || !row.ProductTypeId.HasValue || row.ProductTypeId.Value == Guid.Empty)
        {
            return CreateResult(row, "ScopeUnknown", null, Array.Empty<CatalogRecognitionIntegerCapture>());
        }

        if (row.ManufacturerId.Value != scope.ManufacturerId || row.ProductTypeId.Value != scope.ProductTypeId)
        {
            return CreateResult(row, "OutsideScope", null, Array.Empty<CatalogRecognitionIntegerCapture>());
        }

        if (string.IsNullOrWhiteSpace(row.ProductName))
        {
            return CreateResult(row, "MissingName", null, Array.Empty<CatalogRecognitionIntegerCapture>());
        }

        if (row.ProductName.Length > 2000)
        {
            return CreateResult(row, "NameTooLong", null, Array.Empty<CatalogRecognitionIntegerCapture>());
        }

        var captures = CatalogRecognitionIntegerAlternativesMatcher.Match(row.ProductName, pattern);

        if (captures.Count == 0)
        {
            return CreateResult(row, "NoMatch", null, captures);
        }

        if (captures.Count != 1)
        {
            return CreateResult(row, "AmbiguousMatch", null, captures);
        }

        var proposedValue = captures[0].NormalizedValue;

        if (!decimal.TryParse(proposedValue, NumberStyles.None, CultureInfo.InvariantCulture, out var proposedNumber))
        {
            return CreateResult(row, "NumberOutOfRange", null, captures);
        }

        if (string.IsNullOrWhiteSpace(row.CurrentValue))
        {
            return CreateResult(row, "SuggestedValue", proposedValue, captures);
        }

        if (!decimal.TryParse(row.CurrentValue, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var currentNumber))
        {
            return CreateResult(row, "CurrentValueNotComparable", proposedValue, captures);
        }

        if (currentNumber == proposedNumber)
        {
            return CreateResult(row, "MatchesCurrentValue", proposedValue, captures);
        }

        return CreateResult(row, "DiffersFromCurrentValue", proposedValue, captures);
    }

    private static CatalogRecognitionIntegerRowPreviewResult CreateResult(
        CatalogRecognitionLiteralRowPreviewInput row,
        string status,
        string? proposedValue,
        IReadOnlyList<CatalogRecognitionIntegerCapture> captures)
    {
        return new CatalogRecognitionIntegerRowPreviewResult(
            row.RowId,
            row.RowNumber,
            row.ProductName,
            status,
            row.CurrentValue,
            proposedValue,
            captures);
    }
}