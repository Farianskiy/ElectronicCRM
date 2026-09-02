using System.Globalization;
using System.Text.RegularExpressions;
using ElectronicService.Core.Catalog.Recognition.Configuration;

namespace ElectronicService.Core.Catalog.Recognition.Matching;

internal static class NumericWithUnitRecognitionMatcher
{
    private const int RegexTimeoutMilliseconds = 100;

    public static List<NumericWithUnitRecognitionMatch> FindMatches(
        string productName,
        NumericWithUnitRecognitionSettings settings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productName);
        ArgumentNullException.ThrowIfNull(settings);

        var recognitionRegex = CreateRecognitionRegex(settings);

        if (recognitionRegex is null)
        {
            return [];
        }

        var recognizedMatches = new List<NumericWithUnitRecognitionMatch>();

        foreach (Match match in recognitionRegex.Matches(productName))
        {
            if (!TryGetConfiguredNumericValue(match, settings, out _))
            {
                continue;
            }

            recognizedMatches.Add(
                new NumericWithUnitRecognitionMatch(
                    match.Value.Trim(),
                    match.Groups["value"].Value,
                    match.Index,
                    match.Length));
        }

        return recognizedMatches;
    }

    private static Regex? CreateRecognitionRegex(NumericWithUnitRecognitionSettings settings)
    {
        var escapedUnits = settings.Units
            .Where(unit => !string.IsNullOrWhiteSpace(unit))
            .Select(unit => unit.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(unit => unit.Length)
            .Select(Regex.Escape)
            .ToArray();

        if (escapedUnits.Length == 0)
        {
            return null;
        }

        if (settings.Minimum > settings.Maximum)
        {
            return null;
        }

        var unitPattern = string.Join("|", escapedUnits);

        var numberPattern = settings.AllowDecimal
            ? @"\d+(?:[,.]\d+)?"
            : @"\d+";

        var recognitionPattern = $@"(?<![\d,.])(?<value>{numberPattern})\s*(?:{unitPattern})(?![\p{{L}}\p{{N}}])";

        return new Regex(
            recognitionPattern,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(RegexTimeoutMilliseconds));
    }

    private static bool TryGetConfiguredNumericValue(
        Match match,
        NumericWithUnitRecognitionSettings settings,
        out decimal numericValue)
    {
        var rawNumericValue = match.Groups["value"].Value;
        var normalizedNumericValue = rawNumericValue.Replace(',', '.');

        if (!decimal.TryParse(
            normalizedNumericValue,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out numericValue))
        {
            return false;
        }

        if (!settings.AllowDecimal && numericValue != decimal.Truncate(numericValue))
        {
            return false;
        }

        return numericValue >= settings.Minimum && numericValue <= settings.Maximum;
    }
}