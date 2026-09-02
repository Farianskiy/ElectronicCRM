using System.Text.Json;

namespace ElectronicService.Core.Catalog.Recognition.Configuration;

public static class NumericWithUnitRecognitionSettingsParser
{
    private const int MaximumUnitCount = 32;
    private const int MaximumUnitLength = 20;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public static NumericWithUnitRecognitionSettings? Parse(string configurationJson)
    {
        if (string.IsNullOrWhiteSpace(configurationJson))
        {
            return null;
        }

        try
        {
            var data = JsonSerializer.Deserialize<NumericWithUnitRecognitionSettingsData>(
                configurationJson,
                JsonOptions);

            if (data is null)
            {
                return null;
            }

            if (data.Units is null || data.Units.Length == 0)
            {
                return null;
            }

            if (!data.Minimum.HasValue || !data.Maximum.HasValue || !data.AllowDecimal.HasValue)
            {
                return null;
            }

            if (data.Minimum.Value > data.Maximum.Value)
            {
                return null;
            }

            var normalizedUnits = data.Units
                .Where(unit => !string.IsNullOrWhiteSpace(unit))
                .Select(unit => unit.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (normalizedUnits.Length == 0 || normalizedUnits.Length > MaximumUnitCount)
            {
                return null;
            }

            if (normalizedUnits.Any(unit => unit.Length > MaximumUnitLength))
            {
                return null;
            }

            return new NumericWithUnitRecognitionSettings(
                Array.AsReadOnly(normalizedUnits),
                data.Minimum.Value,
                data.Maximum.Value,
                data.AllowDecimal.Value);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record NumericWithUnitRecognitionSettingsData(
        string[]? Units,
        decimal? Minimum,
        decimal? Maximum,
        bool? AllowDecimal);
}