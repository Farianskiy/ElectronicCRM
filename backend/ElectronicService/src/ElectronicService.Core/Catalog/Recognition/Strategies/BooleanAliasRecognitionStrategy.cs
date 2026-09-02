using System.Text.RegularExpressions;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Configuration;
using ElectronicService.Core.Catalog.Recognition.Models;
using ElectronicService.Domain.Catalog.Recognition;

namespace ElectronicService.Core.Catalog.Recognition.Strategies;

public sealed class BooleanAliasRecognitionStrategy : IBooleanAliasRecognitionStrategy
{
    private const int RegexTimeoutMilliseconds = 100;

    private const decimal Confidence = 0.9900m;

    public string CharacteristicCode => "HAS_THERMAL_RELEASE";

    public CatalogCharacteristicRecognitionStrategyKind StrategyKind => CatalogCharacteristicRecognitionStrategyKind.BooleanAlias;

    public int Priority => 1000;

    public IReadOnlyCollection<CatalogRecognizedCharacteristic> Recognize(string productName)
    {
        return Recognize(productName, BooleanAliasRecognitionDefaults.ThermalRelease);
    }

    public IReadOnlyCollection<CatalogRecognizedCharacteristic> Recognize(string productName, BooleanAliasRecognitionSettings settings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productName);
        ArgumentNullException.ThrowIfNull(settings);

        var matches = new List<BooleanAliasMatch>();

        AddMatches(productName, settings.TrueAliases, true, matches);
        AddMatches(productName, settings.FalseAliases, false, matches);

        return matches
            .OrderBy(match => match.StartIndex)
            .ThenByDescending(match => match.Length)
            .GroupBy(match => new
            {
                match.StartIndex,
                match.Length,
                match.Value
            })
            .Select(group => group.First())
            .Select(CreateCandidate)
            .ToArray();
    }

    private static void AddMatches(string productName, IReadOnlyCollection<string> aliases, bool value, List<BooleanAliasMatch> matches)
    {
        foreach (var alias in aliases)
        {
            var aliasRegex = CreateAliasRegex(alias);

            Match[] aliasMatches;

            try
            {
                aliasMatches = aliasRegex
                    .Matches(productName)
                    .Cast<Match>()
                    .ToArray();
            }
            catch (RegexMatchTimeoutException)
            {
                aliasMatches = [];
            }

            foreach (var match in aliasMatches)
            {
                matches.Add(
                    new BooleanAliasMatch(
                        match.Index,
                        match.Length,
                        match.Value.Trim(),
                        value));
            }
        }
    }

    private static Regex CreateAliasRegex(string alias)
    {
        var aliasParts = alias.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var aliasPattern = string.Join(@"\s+", aliasParts.Select(Regex.Escape));

        var pattern = $@"(?<![\p{{L}}\p{{Nd}}])(?:{aliasPattern})(?![\p{{L}}\p{{Nd}}])";

        return new Regex(
            pattern,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(RegexTimeoutMilliseconds));
    }

    private CatalogRecognizedCharacteristic CreateCandidate(BooleanAliasMatch match)
    {
        var normalizedValue = match.Value
            ? "true"
            : "false";

        var recognizerKey = match.Value
            ? "rule:boolean-alias:thermal-release:true"
            : "rule:boolean-alias:thermal-release:false";

        return new CatalogRecognizedCharacteristic(
            CharacteristicCode,
            match.RawValue,
            normalizedValue,
            Confidence,
            CatalogRecognitionSource.Rule,
            match.StartIndex,
            match.Length,
            Priority,
            recognizerKey);
    }

    private sealed record BooleanAliasMatch(
        int StartIndex,
        int Length,
        string RawValue,
        bool Value);
}