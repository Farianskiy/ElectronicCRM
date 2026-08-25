namespace ElectronicService.Core.Catalog.Recognition.Normalization;

public static class CatalogRecognitionTextNormalizer
{
    public static string NormalizeText(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var normalizedValue = value
            .Trim()
            .ToUpperInvariant()
            .Replace("Ё", "Е", StringComparison.Ordinal);

        return string.Join(
            ' ',
            normalizedValue.Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    public static string NormalizeValue(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return value
            .Trim()
            .ToUpperInvariant()
            .Replace("Ё", "Е", StringComparison.Ordinal)
            .Replace(",", ".", StringComparison.Ordinal);
    }

    public static string NormalizeCode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return value
            .Trim()
            .ToUpperInvariant();
    }

    public static string NormalizeCurve(string value)
    {
        var normalizedValue = NormalizeValue(value);

        return normalizedValue switch
        {
            "С" => "C",
            "В" => "B",
            "Д" => "D",
            _ => normalizedValue
        };
    }
}