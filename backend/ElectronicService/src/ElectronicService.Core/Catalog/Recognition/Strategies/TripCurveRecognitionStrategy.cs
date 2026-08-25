using System.Text.RegularExpressions;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Models;
using ElectronicService.Core.Catalog.Recognition.Normalization;
using ElectronicService.Domain.Catalog.Recognition;

namespace ElectronicService.Core.Catalog.Recognition.Strategies;

public sealed partial class TripCurveRecognitionStrategy : ICatalogCharacteristicRecognitionStrategy
{
    private const int RegexTimeoutMilliseconds = 100;

    public string CharacteristicCode => "CURVE";

    public CatalogCharacteristicRecognitionStrategyKind StrategyKind => CatalogCharacteristicRecognitionStrategyKind.EnumToken;

    public int Priority => 1000;

    public IReadOnlyCollection<CatalogRecognizedCharacteristic> Recognize(string productName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productName);

        var standaloneCandidates = StandaloneCurveRegex()
            .Matches(productName)
            .Cast<Match>()
            .Select(match => CreateCandidate(match, "value", 0.9600m, "rule:trip-curve"));

        var beforeCurrentCandidates = CurveBeforeCurrentRegex()
            .Matches(productName)
            .Cast<Match>()
            .Select(match => CreateCandidate(match, "value", 0.9700m, "rule:trip-curve-before-current"));

        return standaloneCandidates
            .Concat(beforeCurrentCandidates)
            .ToArray();
    }

    private CatalogRecognizedCharacteristic CreateCandidate(
        Match match,
        string groupName,
        decimal confidence,
        string recognizerKey)
    {
        return new CatalogRecognizedCharacteristic(
            CharacteristicCode,
            match.Value.Trim(),
            CatalogRecognitionTextNormalizer.NormalizeCurve(match.Groups[groupName].Value),
            confidence,
            CatalogRecognitionSource.Rule,
            match.Index,
            match.Length,
            Priority,
            recognizerKey);
    }

    [GeneratedRegex(
        @"\b(?<value>[BCDСВД])\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        RegexTimeoutMilliseconds)]
    private static partial Regex StandaloneCurveRegex();

    [GeneratedRegex(
        @"\b(?<value>[BCDСВД])\s*\d+",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        RegexTimeoutMilliseconds)]
    private static partial Regex CurveBeforeCurrentRegex();
}