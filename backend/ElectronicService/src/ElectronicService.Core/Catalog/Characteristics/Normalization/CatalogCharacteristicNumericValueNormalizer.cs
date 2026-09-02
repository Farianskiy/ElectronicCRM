using System.Globalization;
using System.Text;

namespace ElectronicService.Core.Catalog.Characteristics.Normalization;

public static class CatalogCharacteristicNumericValueNormalizer
{
    private const string BreakingCapacityCharacteristicCode =
        "BREAKING_CAPACITY";

    private const decimal AmperesPerKiloampere = 1000m;

    private const decimal BreakingCapacityAmpereInputThreshold = 1000m;

    public static bool TryNormalize(
        string characteristicCode,
        string rawValue,
        out decimal normalizedValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(characteristicCode);

        normalizedValue = default;

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        var numberSource = NormalizeNumberSource(rawValue);

        if (!decimal.TryParse(
                numberSource,
                NumberStyles.AllowLeadingSign |
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out var parsedValue))
        {
            return false;
        }

        normalizedValue = Normalize(
            characteristicCode,
            parsedValue);

        return true;
    }

    public static bool TryNormalizeToString(
        string characteristicCode,
        string rawValue,
        out string normalizedValue)
    {
        if (!TryNormalize(
                characteristicCode,
                rawValue,
                out var normalizedNumber))
        {
            normalizedValue = string.Empty;

            return false;
        }

        normalizedValue = normalizedNumber.ToString(
            "0.############################",
            CultureInfo.InvariantCulture);

        return true;
    }

    public static decimal Normalize(
        string characteristicCode,
        decimal value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(characteristicCode);

        var normalizedCharacteristicCode =
            NormalizeCharacteristicCode(characteristicCode);

        if (string.Equals(
                normalizedCharacteristicCode,
                BreakingCapacityCharacteristicCode,
                StringComparison.Ordinal) &&
            value >= BreakingCapacityAmpereInputThreshold)
        {
            return value / AmperesPerKiloampere;
        }

        return value;
    }

    private static string NormalizeNumberSource(string rawValue)
    {
        var builder = new StringBuilder(rawValue.Length);

        foreach (var character in rawValue.Trim())
        {
            if (char.IsWhiteSpace(character))
            {
                continue;
            }

            builder.Append(character == ',' ? '.' : character);
        }

        return builder.ToString();
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