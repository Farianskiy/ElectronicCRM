using System.Globalization;
using System.Text.RegularExpressions;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Models;
using ElectronicService.Core.Catalog.Recognition.Normalization;
using ElectronicService.Domain.Catalog.Recognition;

namespace ElectronicService.Core.Catalog.Recognition.Strategies;

public sealed partial class PoleCountRecognitionStrategy : ICatalogCharacteristicRecognitionStrategy
{
    private const int RegexTimeoutMilliseconds = 100;

    public string CharacteristicCode => "POLES";

    public CatalogCharacteristicRecognitionStrategyKind StrategyKind => CatalogCharacteristicRecognitionStrategyKind.PoleCount;

    public int Priority => 1000;

    public IReadOnlyCollection<CatalogRecognizedCharacteristic> Recognize(string productName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productName);

        return PoleCountRegex()
            .Matches(productName)
            .Cast<Match>()
            .Select(match => new CatalogRecognizedCharacteristic(
                CharacteristicCode,
                match.Value.Trim(),
                NormalizePoleCount(match),
                0.9900m,
                CatalogRecognitionSource.Rule,
                match.Index,
                match.Length,
                Priority,
                "rule:pole-count"))
            .ToArray();
    }

    private static string NormalizePoleCount(Match match)
    {
        var normalizedValue = CatalogRecognitionTextNormalizer.NormalizeValue(match.Groups["value"].Value);

        if (!match.Groups["neutral"].Success)
        {
            return normalizedValue;
        }

        if (!int.TryParse(normalizedValue, NumberStyles.None, CultureInfo.InvariantCulture, out var poleCount))
        {
            return normalizedValue;
        }

        return (poleCount + 1).ToString(CultureInfo.InvariantCulture);
    }

    [GeneratedRegex(
        @"(?<value>\d+)\s*(?:П|P|Р)(?<neutral>\s*\+\s*(?:Н|N))?\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        RegexTimeoutMilliseconds)]
    private static partial Regex PoleCountRegex();
}