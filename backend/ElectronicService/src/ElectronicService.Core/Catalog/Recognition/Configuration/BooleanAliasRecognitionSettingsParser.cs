using System.Text.Json;

namespace ElectronicService.Core.Catalog.Recognition.Configuration;

public static class BooleanAliasRecognitionSettingsParser
{
    private const int MaximumAliasCountPerValue = 64;

    private const int MaximumAliasLength = 120;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public static BooleanAliasRecognitionSettings? Parse(string configurationJson)
    {
        if (string.IsNullOrWhiteSpace(configurationJson))
        {
            return null;
        }

        try
        {
            var data = JsonSerializer.Deserialize<BooleanAliasRecognitionSettingsData>(configurationJson, JsonOptions);

            if (data is null)
            {
                return null;
            }

            var trueAliases = NormalizeAliases(data.TrueAliases);

            if (trueAliases is null)
            {
                return null;
            }

            var falseAliases = NormalizeAliases(data.FalseAliases);

            if (falseAliases is null)
            {
                return null;
            }

            var aliasesUsedForBothValues = trueAliases.Intersect(falseAliases, StringComparer.OrdinalIgnoreCase).Any();

            if (aliasesUsedForBothValues)
            {
                return null;
            }

            return new BooleanAliasRecognitionSettings(
                Array.AsReadOnly(trueAliases),
                Array.AsReadOnly(falseAliases));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string[]? NormalizeAliases(string[]? aliases)
    {
        if (aliases is null || aliases.Length == 0 || aliases.Length > MaximumAliasCountPerValue)
        {
            return null;
        }

        if (aliases.Any(string.IsNullOrWhiteSpace))
        {
            return null;
        }

        var normalizedAliases = aliases
            .Select(NormalizeWhitespace)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalizedAliases.Length == 0 || normalizedAliases.Length > MaximumAliasCountPerValue)
        {
            return null;
        }

        if (normalizedAliases.Any(alias => alias.Length > MaximumAliasLength))
        {
            return null;
        }

        return normalizedAliases;
    }

    private static string NormalizeWhitespace(string value)
    {
        return string.Join(
            " ",
            value.Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private sealed record BooleanAliasRecognitionSettingsData(
        string[]? TrueAliases,
        string[]? FalseAliases);
}