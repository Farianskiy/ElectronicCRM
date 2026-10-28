namespace ElectronicService.Core.Catalog.ImportBatches.PreviewExampleFill;

public static class CatalogImportExampleFillTokenMatcher
{
    public static int FindStart(string productName, string rawValue)
    {
        return FindStart(productName, rawValue, null);
    }

    public static int FindStart(
        string productName,
        string rawValue,
        string? characteristicUnit)
    {
        ArgumentNullException.ThrowIfNull(productName);
        ArgumentNullException.ThrowIfNull(rawValue);

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return -1;
        }

        var startIndex = 0;
        while (startIndex <= productName.Length - rawValue.Length)
        {
            var index = productName.IndexOf(
                rawValue,
                startIndex,
                StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                return -1;
            }

            var beforeIsBoundary = index == 0
                || !char.IsLetterOrDigit(productName[index - 1])
                || !char.IsLetterOrDigit(rawValue[0]);
            var end = index + rawValue.Length;
            var afterIsBoundary = end == productName.Length
                || !char.IsLetterOrDigit(productName[end])
                || !char.IsLetterOrDigit(rawValue[^1])
                || IsImmediatelyFollowedByUnit(
                    productName,
                    end,
                    rawValue,
                    characteristicUnit);

            if (beforeIsBoundary && afterIsBoundary)
            {
                return index;
            }

            startIndex = index + 1;
        }

        return -1;
    }

    private static bool IsImmediatelyFollowedByUnit(
        string productName,
        int valueEnd,
        string rawValue,
        string? characteristicUnit)
    {
        if (!char.IsDigit(rawValue[^1]) ||
            string.IsNullOrWhiteSpace(characteristicUnit))
        {
            return false;
        }

        var unit = characteristicUnit.Trim();
        if (unit.Length == 0 ||
            valueEnd + unit.Length > productName.Length)
        {
            return false;
        }

        var nameUnit = productName.Substring(valueEnd, unit.Length);
        if (!string.Equals(nameUnit, unit, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(
                NormalizeTechnicalUnit(nameUnit),
                NormalizeTechnicalUnit(unit),
                StringComparison.Ordinal))
        {
            return false;
        }

        var unitEnd = valueEnd + unit.Length;
        return unitEnd == productName.Length ||
            !char.IsLetterOrDigit(productName[unitEnd]);
    }

    private static string NormalizeTechnicalUnit(string value)
    {
        return value
            .ToUpperInvariant()
            .Replace('А', 'A')
            .Replace('К', 'K')
            .Replace('М', 'M')
            .Replace('В', 'V');
    }
}
