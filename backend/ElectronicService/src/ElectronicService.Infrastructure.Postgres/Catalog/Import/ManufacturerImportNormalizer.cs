namespace ElectronicService.Infrastructure.Postgres.Catalog.Import;

internal static class ManufacturerImportNormalizer
{
    private const string UnknownManufacturer = "Не указан";

    private static readonly Dictionary<string, string> CanonicalManufacturerNames = new(StringComparer.Ordinal)
    {
        ["CHINT"] = "CHINT",
        ["ЧИНТ"] = "CHINT",
        ["ЧЕНТ"] = "CHINT",
        ["ЧНТ"] = "CHINT",
        ["ЧАНТ"] = "CHINT",

        ["IEK"] = "IEK",
        ["ИЕК"] = "IEK",
        ["ИЭК"] = "IEK",

        ["EKF"] = "EKF",

        ["ABB"] = "ABB",
        ["АВВ"] = "ABB",

        ["DEKRAFT"] = "DEKraft",

        ["КЭАЗ"] = "КЭАЗ",
        ["КЕАЗ"] = "КЭАЗ"
    };

    public static ManufacturerNormalizationResult NormalizeManufacturerName(
        string? rawManufacturerName)
    {
        if (string.IsNullOrWhiteSpace(rawManufacturerName))
        {
            return new ManufacturerNormalizationResult(
                RawName: rawManufacturerName ?? string.Empty,
                NormalizedName: UnknownManufacturer,
                WasChanged: true);
        }

        var trimmedName = rawManufacturerName.Trim();
        var normalizedName = NormalizeText(trimmedName);

        if (CanonicalManufacturerNames.TryGetValue(normalizedName, out var canonicalName))
        {
            return new ManufacturerNormalizationResult(
                RawName: trimmedName,
                NormalizedName: canonicalName,
                WasChanged: !string.Equals(trimmedName, canonicalName, StringComparison.Ordinal));
        }

        var withoutIndustrialSeries = RemoveIndustrialSeriesSuffix(normalizedName);

        if (!string.Equals(withoutIndustrialSeries, normalizedName, StringComparison.Ordinal)
            && CanonicalManufacturerNames.TryGetValue(withoutIndustrialSeries, out var canonicalWithoutIndustrialSeries))
        {
            return new ManufacturerNormalizationResult(
                RawName: trimmedName,
                NormalizedName: canonicalWithoutIndustrialSeries,
                WasChanged: true);
        }

        return new ManufacturerNormalizationResult(
            RawName: trimmedName,
            NormalizedName: trimmedName,
            WasChanged: false);
    }

    private static string RemoveIndustrialSeriesSuffix(string normalizedName)
    {
        const string industrialSeriesSuffix = " ПРОМ СЕРИЯ";

        if (!normalizedName.EndsWith(industrialSeriesSuffix, StringComparison.Ordinal))
        {
            return normalizedName;
        }

        return normalizedName[..^industrialSeriesSuffix.Length].Trim();
    }

    private static string NormalizeText(string value)
    {
        return value
            .Trim()
            .ToUpperInvariant()
            .Replace("Ё", "Е", StringComparison.Ordinal);
    }
}