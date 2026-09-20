namespace ElectronicService.Core.Catalog.Characteristics.Normalization;

public static class CatalogPoleConfigurationNormalizer
{
    public const string CharacteristicCode = "POLES";

    private static readonly IReadOnlyList<string> OnePoleSearchValues = ["1P"];
    private static readonly IReadOnlyList<string> OnePoleWithNeutralSearchValues = ["1P+N"];
    private static readonly IReadOnlyList<string> TwoPoleSearchValues = ["2P", "1P+N"];
    private static readonly IReadOnlyList<string> ThreePoleSearchValues = ["3P"];
    private static readonly IReadOnlyList<string> ThreePoleWithNeutralSearchValues = ["3P+N"];
    private static readonly IReadOnlyList<string> FourPoleSearchValues = ["4P", "3P+N"];

    public static bool TryNormalize(string? rawValue, out string normalizedValue)
    {
        normalizedValue = string.Empty;

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        var compactValue = string.Concat(rawValue.Trim().ToUpperInvariant().Where(character => !char.IsWhiteSpace(character)))
            .Replace('Р', 'P')
            .Replace('П', 'P')
            .Replace('Н', 'N');

        normalizedValue = compactValue switch
        {
            "1" or "1P" => "1P",
            "1P+N" => "1P+N",
            "2" or "2P" => "2P",
            "3" or "3P" => "3P",
            "3P+N" => "3P+N",
            "4" or "4P" => "4P",
            _ => string.Empty
        };

        return normalizedValue.Length > 0;
    }

    public static IReadOnlyList<string> ExpandForSearch(string? rawValue)
    {
        if (!TryNormalize(rawValue, out var normalizedValue))
        {
            return [];
        }

        return normalizedValue switch
        {
            "1P" => OnePoleSearchValues,
            "1P+N" => OnePoleWithNeutralSearchValues,
            "2P" => TwoPoleSearchValues,
            "3P" => ThreePoleSearchValues,
            "3P+N" => ThreePoleWithNeutralSearchValues,
            "4P" => FourPoleSearchValues,
            _ => []
        };
    }
}