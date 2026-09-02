using System.Text.RegularExpressions;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Models;
using ElectronicService.Domain.Catalog.Recognition;

namespace ElectronicService.Core.Catalog.Recognition.Strategies;

public sealed partial class IpRatingRecognitionStrategy : ICatalogCharacteristicRecognitionStrategy
{
    private const int RegexTimeoutMilliseconds = 100;

    public string CharacteristicCode => "IP_RATING";

    public CatalogCharacteristicRecognitionStrategyKind StrategyKind => CatalogCharacteristicRecognitionStrategyKind.EnumToken;

    public int Priority => 1000;

    public IReadOnlyCollection<CatalogRecognizedCharacteristic> Recognize(string productName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productName);

        return IpRatingRegex()
            .Matches(productName)
            .Cast<Match>()
            .Select(match => new CatalogRecognizedCharacteristic(
                CharacteristicCode,
                match.Value.Trim(),
                $"IP{match.Groups["value"].Value}",
                0.9900m,
                CatalogRecognitionSource.Rule,
                match.Index,
                match.Length,
                Priority,
                "rule:ip-rating"))
            .ToArray();
    }

    [GeneratedRegex(
        @"\bIP\s*(?<value>\d{2})\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        RegexTimeoutMilliseconds)]
    private static partial Regex IpRatingRegex();
}