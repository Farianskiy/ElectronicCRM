using System.Globalization;
using ElectronicService.Core.Catalog.Characteristics.Normalization;

namespace ElectronicService.Core.Catalog.Recognition.Normalization;

public static class CatalogRecognitionValueNormalizer
{
    public static string Normalize(
        string characteristicCode,
        string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            characteristicCode);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            value);

        var normalizedCode =
            CatalogRecognitionTextNormalizer.NormalizeCode(
                characteristicCode);

        return normalizedCode switch
        {
            "RATED_CURRENT"
                => NormalizeNumber(value),

            "POLES"
                => NormalizeNumber(value),

            "BREAKING_CAPACITY"
                => NormalizeBreakingCapacity(value),

            "LEAKAGE_CURRENT"
                => NormalizeNumber(value),

            "CURVE"
                => CatalogRecognitionTextNormalizer
                    .NormalizeCurve(value),

            "IP_RATING"
                => NormalizeIpRating(value),

            _
                => CatalogRecognitionTextNormalizer
                    .NormalizeValue(value)
        };
    }

    private static string NormalizeNumber(
        string value)
    {
        var normalizedValue =
            CatalogRecognitionTextNormalizer.NormalizeValue(
                value);

        if (!decimal.TryParse(
                normalizedValue,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var number))
        {
            return normalizedValue;
        }

        return number.ToString(
            "0.############################",
            CultureInfo.InvariantCulture);
    }

    private static string NormalizeBreakingCapacity(
        string value)
    {
        if (CatalogCharacteristicNumericValueNormalizer
            .TryNormalizeToString(
                "BREAKING_CAPACITY",
                value,
                out var normalizedValue))
        {
            return normalizedValue;
        }

        return NormalizeNumber(value);
    }

    private static string NormalizeIpRating(
        string value)
    {
        var normalizedValue =
            CatalogRecognitionTextNormalizer.NormalizeValue(
                value);

        var compactValue = string.Concat(
            normalizedValue.Where(character =>
                !char.IsWhiteSpace(character)));

        if (compactValue.All(char.IsDigit))
        {
            return $"IP{compactValue}";
        }

        return compactValue;
    }
}