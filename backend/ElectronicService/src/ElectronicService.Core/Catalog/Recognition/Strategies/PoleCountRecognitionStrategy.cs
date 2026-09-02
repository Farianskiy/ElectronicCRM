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
                CatalogRecognitionTextNormalizer.NormalizeValue(match.Groups["value"].Value),
                0.9900m,
                CatalogRecognitionSource.Rule,
                match.Index,
                match.Length,
                Priority,
                "rule:pole-count"))
            .ToArray();
    }

    [GeneratedRegex(
        @"(?<value>\d+)\s*(?:П|P|Р)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        RegexTimeoutMilliseconds)]
    private static partial Regex PoleCountRegex();
}