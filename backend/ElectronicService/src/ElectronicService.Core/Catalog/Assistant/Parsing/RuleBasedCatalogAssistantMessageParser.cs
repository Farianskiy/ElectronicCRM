using System.Text.RegularExpressions;
using ElectronicService.Core.Catalog.Assistant.Abstractions;
using ElectronicService.Core.Catalog.Assistant.AskCatalogAssistant;
using ElectronicService.Core.Catalog.Dictionaries.Abstractions;
using ElectronicService.Core.Catalog.Products.SearchProducts;
using ElectronicService.Domain.Catalog.Dictionaries;
using ElectronicService.Core.Catalog.Dictionaries.GetTerms;

namespace ElectronicService.Core.Catalog.Assistant.Parsing;

public sealed partial class RuleBasedCatalogAssistantMessageParser
    : ICatalogAssistantMessageParser
{
    private readonly ICatalogDictionaryReader _dictionaryReader;
    private readonly ICatalogAssistantUnknownTermResolver _unknownTermResolver;

    public RuleBasedCatalogAssistantMessageParser(
    ICatalogDictionaryReader dictionaryReader,
    ICatalogAssistantUnknownTermResolver unknownTermResolver)
    {
        _dictionaryReader = dictionaryReader;
        _unknownTermResolver = unknownTermResolver;
    }

    public async Task<CatalogAssistantParsedRequest> ParseAsync(
        string message,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        var normalizedMessage = NormalizeText(message);

        var intent = ResolveIntent(normalizedMessage);

        var terms = await _dictionaryReader
            .GetApprovedTermsAsync(cancellationToken)
            .ConfigureAwait(false);

        string? search = null;
        string? productTypeCode = null;
        string? manufacturer = null;

        var characteristics = new List<SearchProductCharacteristicFilter>();

        foreach (var term in terms)
        {
            if (!normalizedMessage.Contains(
                    term.NormalizedPhrase,
                    StringComparison.Ordinal))
            {
                continue;
            }

            if (!Enum.TryParse<CatalogDictionaryTermKind>(
                    term.Kind,
                    ignoreCase: true,
                    out var kind))
            {
                continue;
            }

            switch (kind)
            {
                case CatalogDictionaryTermKind.Manufacturer:
                    manufacturer ??= term.TargetValue;
                    break;

                case CatalogDictionaryTermKind.ProductType:
                    productTypeCode ??= term.TargetValue;
                    break;

                case CatalogDictionaryTermKind.Characteristic:
                    if (!string.IsNullOrWhiteSpace(term.TargetCode))
                    {
                        AddOrReplaceCharacteristic(
                            characteristics,
                            term.TargetCode,
                            term.TargetValue);
                    }

                    break;

                case CatalogDictionaryTermKind.SearchToken:
                    search ??= term.TargetValue;
                    break;
            }
        }

        ExtractRegexCharacteristics(
            normalizedMessage,
            characteristics);

        search ??= ExtractSearchToken(normalizedMessage);

        var unknownPhrase = FindFirstUnknownPhrase(
            normalizedMessage,
            terms);

        var clarification = unknownPhrase is null
            ? null
            : await _unknownTermResolver
                .ResolveAsync(unknownPhrase, cancellationToken)
                .ConfigureAwait(false);

        return new CatalogAssistantParsedRequest(
            intent,
            search,
            productTypeCode,
            manufacturer,
            characteristics,
            clarification);
    }

    private static CatalogAssistantIntent ResolveIntent(string normalizedMessage)
    {
        if (normalizedMessage.Contains("ЗАМЕН", StringComparison.Ordinal)
            || normalizedMessage.Contains("АНАЛОГ", StringComparison.Ordinal)
            || normalizedMessage.Contains("ПОДБЕРИ", StringComparison.Ordinal))
        {
            return CatalogAssistantIntent.SearchReplacements;
        }

        if (normalizedMessage.Contains("НАЙДИ", StringComparison.Ordinal)
            || normalizedMessage.Contains("ПОКАЖИ", StringComparison.Ordinal)
            || normalizedMessage.Contains("ЕСТЬ", StringComparison.Ordinal)
            || normalizedMessage.Contains("ТОВАР", StringComparison.Ordinal))
        {
            return CatalogAssistantIntent.SearchProducts;
        }

        return CatalogAssistantIntent.SearchProducts;
    }

    private static void ExtractRegexCharacteristics(
        string normalizedMessage,
        List<SearchProductCharacteristicFilter> characteristics)
    {
        var polesMatch = PolesRegex().Match(normalizedMessage);

        if (polesMatch.Success)
        {
            AddOrReplaceCharacteristic(
                characteristics,
                "POLES",
                polesMatch.Groups["value"].Value);
        }

        var breakingCapacityMatch = BreakingCapacityRegex().Match(normalizedMessage);

        if (breakingCapacityMatch.Success)
        {
            AddOrReplaceCharacteristic(
                characteristics,
                "BREAKING_CAPACITY",
                breakingCapacityMatch.Groups["value"].Value);
        }

        var leakageCurrentMatch = LeakageCurrentRegex().Match(normalizedMessage);

        if (leakageCurrentMatch.Success)
        {
            AddOrReplaceCharacteristic(
                characteristics,
                "LEAKAGE_CURRENT",
                leakageCurrentMatch.Groups["value"].Value);
        }

        var ratedCurrentMatch = RatedCurrentRegex().Match(normalizedMessage);

        if (ratedCurrentMatch.Success)
        {
            AddOrReplaceCharacteristic(
                characteristics,
                "RATED_CURRENT",
                ratedCurrentMatch.Groups["value"].Value);
        }

        var curveMatch = CurveRegex().Match(normalizedMessage);

        if (curveMatch.Success)
        {
            AddOrReplaceCharacteristic(
                characteristics,
                "CURVE",
                NormalizeCurve(curveMatch.Groups["value"].Value));
        }

        var curveBeforeCurrentMatch = CurveBeforeCurrentRegex().Match(normalizedMessage);

        if (curveBeforeCurrentMatch.Success)
        {
            AddOrReplaceCharacteristic(
                characteristics,
                "CURVE",
                NormalizeCurve(curveBeforeCurrentMatch.Groups["value"].Value));
        }

        var ipRatingMatch = IpRatingRegex().Match(normalizedMessage);

        if (ipRatingMatch.Success)
        {
            AddOrReplaceCharacteristic(
                characteristics,
                "IP_RATING",
                $"IP{ipRatingMatch.Groups["value"].Value}");
        }
    }

    private static string? ExtractSearchToken(string normalizedMessage)
    {
        var seriesMatch = SeriesTokenRegex().Match(normalizedMessage);

        if (!seriesMatch.Success)
        {
            return null;
        }

        var value = seriesMatch.Groups["value"].Value;

        if (value.StartsWith("IP", StringComparison.Ordinal))
        {
            return null;
        }

        return value;
    }

    private static void AddOrReplaceCharacteristic(
        List<SearchProductCharacteristicFilter> characteristics,
        string code,
        string value)
    {
        var normalizedCode = NormalizeText(code);
        var normalizedValue = NormalizeCharacteristicValue(value);

        var existingIndex = characteristics.FindIndex(characteristic =>
            string.Equals(
                characteristic.Code,
                normalizedCode,
                StringComparison.Ordinal));

        var characteristic = new SearchProductCharacteristicFilter(
            normalizedCode,
            normalizedValue);

        if (existingIndex < 0)
        {
            characteristics.Add(characteristic);
            return;
        }

        characteristics[existingIndex] = characteristic;
    }

    private static string NormalizeCharacteristicValue(string value)
    {
        return value
            .Trim()
            .ToUpperInvariant()
            .Replace("Ё", "Е", StringComparison.Ordinal)
            .Replace(",", ".", StringComparison.Ordinal);
    }

    private static string NormalizeText(string value)
    {
        return value
            .Trim()
            .ToUpperInvariant()
            .Replace("Ё", "Е", StringComparison.Ordinal);
    }

    private static string NormalizeCurve(string value)
    {
        var normalizedValue = NormalizeText(value);

        return normalizedValue switch
        {
            "С" => "C",
            "В" => "B",
            _ => normalizedValue
        };
    }

    private const int RegexTimeoutMilliseconds = 100;

    [GeneratedRegex(
        @"(?<value>\d+)\s*(?:П|P)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        RegexTimeoutMilliseconds)]
    private static partial Regex PolesRegex();

    [GeneratedRegex(
        @"(?<value>\d+(?:[,.]\d+)?)\s*(?:К|K)\s*(?:А|A)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        RegexTimeoutMilliseconds)]
    private static partial Regex BreakingCapacityRegex();

    [GeneratedRegex(
        @"(?<value>\d+(?:[,.]\d+)?)\s*(?:М|M)\s*(?:А|A)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        RegexTimeoutMilliseconds)]
    private static partial Regex LeakageCurrentRegex();

    [GeneratedRegex(
        @"(?<value>\d+(?:[,.]\d+)?)\s*(?:А|A)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        RegexTimeoutMilliseconds)]
    private static partial Regex RatedCurrentRegex();

    [GeneratedRegex(
        @"\b(?<value>[BCDСВ])\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        RegexTimeoutMilliseconds)]
    private static partial Regex CurveRegex();

    [GeneratedRegex(
        @"\b(?<value>[BCDСВ])\s*\d+",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        RegexTimeoutMilliseconds)]
    private static partial Regex CurveBeforeCurrentRegex();

    [GeneratedRegex(
        @"\bIP\s*(?<value>\d{2})\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        RegexTimeoutMilliseconds)]
    private static partial Regex IpRatingRegex();

    [GeneratedRegex(
        @"\b(?<value>[А-ЯA-Z]{1,8}\d+[А-ЯA-Z0-9\-]*)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        RegexTimeoutMilliseconds)]
    private static partial Regex SeriesTokenRegex();

    private static string? FindFirstUnknownPhrase(
    string normalizedMessage,
    IReadOnlyCollection<CatalogDictionaryTermResult> terms)
    {
        var recognizedWords = terms
            .Where(term => normalizedMessage.Contains(
                term.NormalizedPhrase,
                StringComparison.Ordinal))
            .SelectMany<CatalogDictionaryTermResult, string>(term =>
                term.NormalizedPhrase.Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var word in WordRegex()
                    .Matches(normalizedMessage)
                    .Cast<Match>()
                    .Select(match => match.Value))
        {
            if (word.Length < 3)
            {
                continue;
            }

            if (word.Any(char.IsDigit))
            {
                continue;
            }

            if (recognizedWords.Contains(word))
            {
                continue;
            }

            if (IgnoredWords.Contains(word))
            {
                continue;
            }

            return word;
        }

        return null;
    }

    private static readonly HashSet<string> IgnoredWords = new(StringComparer.Ordinal)
    {
        "НАЙДИ",
        "ПОКАЖИ",
        "ЕСТЬ",
        "ТОВАР",
        "ТОВАРЫ",
        "ПОДБЕРИ",
        "ЗАМЕНУ",
        "ЗАМЕНА",
        "АНАЛОГ",
        "АНАЛОГИ",
        "МНЕ",
        "НУЖЕН",
        "НУЖНО",
        "НУЖНА",
        "НА",
        "С",
        "СО",
        "ДЛЯ",
        "И",
        "ИЛИ",
        "ПО",
        "СЕРИИ",
        "СЕРИЯ",
        "ТИПА"
    };

    [GeneratedRegex(
        @"[А-ЯA-Z0-9\-]+",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        RegexTimeoutMilliseconds)]
    private static partial Regex WordRegex();
}