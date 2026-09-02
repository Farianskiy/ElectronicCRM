using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Configuration;
using ElectronicService.Core.Catalog.Recognition.Matching;
using ElectronicService.Core.Catalog.Recognition.Models;
using ElectronicService.Core.Catalog.Recognition.Normalization;
using ElectronicService.Domain.Catalog.Recognition;

namespace ElectronicService.Core.Catalog.Recognition.Strategies;

public sealed class LeakageCurrentRecognitionStrategy : INumericWithUnitRecognitionStrategy
{
    private static readonly NumericWithUnitRecognitionSettings DefaultSettings = new(
        Array.AsReadOnly(
            new[]
            {
                "мА",
                "mA"
            }),
        0m,
        decimal.MaxValue,
        true);

    public string CharacteristicCode => "LEAKAGE_CURRENT";

    public CatalogCharacteristicRecognitionStrategyKind StrategyKind => CatalogCharacteristicRecognitionStrategyKind.NumericWithUnit;

    public int Priority => 1000;

    public IReadOnlyCollection<CatalogRecognizedCharacteristic> Recognize(string productName)
    {
        return Recognize(productName, DefaultSettings);
    }

    public IReadOnlyCollection<CatalogRecognizedCharacteristic> Recognize(
        string productName,
        NumericWithUnitRecognitionSettings settings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productName);
        ArgumentNullException.ThrowIfNull(settings);

        return NumericWithUnitRecognitionMatcher
            .FindMatches(productName, settings)
            .Select(match => new CatalogRecognizedCharacteristic(
                CharacteristicCode,
                match.RawValue,
                CatalogRecognitionTextNormalizer.NormalizeValue(match.RawNumericValue),
                0.9900m,
                CatalogRecognitionSource.Rule,
                match.StartIndex,
                match.Length,
                Priority,
                "rule:leakage-current"))
            .ToArray();
    }
}