using System.Text;

namespace ElectronicService.Core.Catalog.Characteristics.Normalization;

public static class CatalogCharacteristicBooleanValueNormalizer
{
    private const string HasThermalReleaseCharacteristicCode = "HAS_THERMAL_RELEASE";

    public static bool TryNormalize(string characteristicCode, string rawValue, out bool normalizedValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(characteristicCode);

        normalizedValue = default;

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        var normalizedCharacteristicCode = NormalizeCharacteristicCode(characteristicCode);
        var normalizedSource = NormalizeValueSource(rawValue);

        switch (normalizedSource)
        {
            case "ДА":
            case "YES":
            case "TRUE":
            case "1":
            case "+":
            case "ЕСТЬ":
            case "ИМЕЕТСЯ":
            case "ПРИСУТСТВУЕТ":
                normalizedValue = true;
                return true;

            case "НЕТ":
            case "NO":
            case "FALSE":
            case "0":
            case "-":
            case "ОТСУТСТВУЕТ":
            case "НЕИМЕЕТСЯ":
                normalizedValue = false;
                return true;
        }

        if (!string.Equals(normalizedCharacteristicCode, HasThermalReleaseCharacteristicCode, StringComparison.Ordinal))
        {
            return false;
        }

        switch (normalizedSource)
        {
            case "ТР":
            case "СТР":
            case "СРАСЦЕПИТЕЛЕМ":
            case "СТЕПЛОВЫМРАСЦЕПИТЕЛЕМ":
            case "ТЕПЛОВОЙРАСЦЕПИТЕЛЬ":
            case "ТЕПЛОВЫЙРАСЦЕПИТЕЛЬ":
                normalizedValue = true;
                return true;

            case "БЕЗТР":
            case "НЕТТР":
            case "БЕЗРАСЦЕПИТЕЛЯ":
            case "БЕЗТЕПЛОВОГОРАСЦЕПИТЕЛЯ":
                normalizedValue = false;
                return true;

            default:
                return false;
        }
    }

    public static bool TryNormalizeToString(string characteristicCode, string rawValue, out string normalizedValue)
    {
        if (!TryNormalize(characteristicCode, rawValue, out var booleanValue))
        {
            normalizedValue = string.Empty;
            return false;
        }

        normalizedValue = booleanValue
            ? "true"
            : "false";

        return true;
    }

    private static string NormalizeCharacteristicCode(string characteristicCode)
    {
        return characteristicCode
            .Trim()
            .ToUpperInvariant()
            .Replace("Ё", "Е", StringComparison.Ordinal)
            .Replace(" ", "_", StringComparison.Ordinal)
            .Replace("-", "_", StringComparison.Ordinal);
    }

    private static string NormalizeValueSource(string rawValue)
    {
        var trimmedValue = rawValue.Trim();

        if (trimmedValue is "+" or "-")
        {
            return trimmedValue;
        }

        var upperValue = trimmedValue
            .ToUpperInvariant()
            .Replace("Ё", "Е", StringComparison.Ordinal);

        var builder = new StringBuilder(upperValue.Length);

        foreach (var character in upperValue)
        {
            if (char.IsWhiteSpace(character))
            {
                continue;
            }

            if (character is '.' or '_' or '-')
            {
                continue;
            }

            builder.Append(character);
        }

        return builder.ToString();
    }
}