namespace ElectronicService.Core.Catalog.Characteristics.Normalization;

public static class CatalogCharacteristicTextValueNormalizer
{
    private const string ProductSeriesCharacteristicCode = "PRODUCT_SERIES";

    public static bool TryNormalizeToString(
        string characteristicCode,
        string rawValue,
        string? manufacturerName,
        out string normalizedValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(characteristicCode);

        normalizedValue = string.Empty;

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        normalizedValue = rawValue.Trim();

        if (!string.Equals(
                NormalizeCharacteristicCode(characteristicCode),
                ProductSeriesCharacteristicCode,
                StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(manufacturerName))
        {
            return true;
        }

        var normalizedManufacturerName = manufacturerName.Trim();

        if (!normalizedValue.StartsWith(
                normalizedManufacturerName,
                StringComparison.OrdinalIgnoreCase) ||
            normalizedValue.Length == normalizedManufacturerName.Length)
        {
            return true;
        }

        var seriesStartIndex = normalizedManufacturerName.Length;

        if (!IsManufacturerSeparator(normalizedValue[seriesStartIndex]))
        {
            return true;
        }

        while (seriesStartIndex < normalizedValue.Length &&
               IsManufacturerSeparator(normalizedValue[seriesStartIndex]))
        {
            seriesStartIndex++;
        }

        if (seriesStartIndex >= normalizedValue.Length)
        {
            return true;
        }

        normalizedValue = normalizedValue[seriesStartIndex..].Trim();

        return normalizedValue.Length > 0;
    }

    private static bool IsManufacturerSeparator(char character)
    {
        return char.IsWhiteSpace(character) ||
               character is '-' or '–' or '—' or ':' or '/' or '\\';
    }

    private static string NormalizeCharacteristicCode(
        string characteristicCode)
    {
        return characteristicCode
            .Trim()
            .ToUpperInvariant()
            .Replace("Ё", "Е", StringComparison.Ordinal)
            .Replace(" ", "_", StringComparison.Ordinal)
            .Replace("-", "_", StringComparison.Ordinal);
    }
}