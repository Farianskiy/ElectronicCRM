using System.Text.RegularExpressions;
using ElectronicService.Core.Catalog.Assistant.Abstractions;
using ElectronicService.Core.Catalog.Assistant.AskCatalogAssistant;
using ElectronicService.Core.Catalog.Dictionaries.Abstractions;
using ElectronicService.Core.Catalog.Dictionaries.GetTerms;
using ElectronicService.Core.Catalog.Products.SearchProducts;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Models;
using ElectronicService.Domain.Catalog.Dictionaries;

namespace ElectronicService.Core.Catalog.Assistant.Parsing;

public sealed partial class RuleBasedCatalogAssistantMessageParser : ICatalogAssistantMessageParser
{
    private const int RegexTimeoutMilliseconds = 100;

    private readonly ICatalogDictionaryReader _dictionaryReader;
    private readonly ICatalogAssistantUnknownTermResolver _unknownTermResolver;
    private readonly ICatalogProductNameRecognitionService _productNameRecognitionService;

    public RuleBasedCatalogAssistantMessageParser(ICatalogDictionaryReader dictionaryReader, ICatalogAssistantUnknownTermResolver unknownTermResolver, ICatalogProductNameRecognitionService productNameRecognitionService)
    {
        _dictionaryReader = dictionaryReader;
        _unknownTermResolver = unknownTermResolver;
        _productNameRecognitionService = productNameRecognitionService;
    }

    public async Task<CatalogAssistantParsedRequest> ParseAsync(string message, CancellationToken cancellationToken = default)
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
            if (!normalizedMessage.Contains(term.NormalizedPhrase, StringComparison.Ordinal))
            {
                continue;
            }

            if (!Enum.TryParse<CatalogDictionaryTermKind>(term.Kind, ignoreCase: true, out var kind))
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

        var recognitionResult = await _productNameRecognitionService
            .RecognizeAsync(
                new CatalogProductNameRecognitionRequest(message),
                cancellationToken)
            .ConfigureAwait(false);

        foreach (var recognizedCharacteristic in recognitionResult.Characteristics)
        {
            AddOrReplaceCharacteristic(
                characteristics,
                recognizedCharacteristic.CharacteristicCode,
                recognizedCharacteristic.NormalizedValue);
        }

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

    private static void AddOrReplaceCharacteristic(List<SearchProductCharacteristicFilter> characteristics, string code, string value)
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

    private static string? FindFirstUnknownPhrase(string normalizedMessage, IReadOnlyCollection<CatalogDictionaryTermResult> terms)
    {
        var recognizedWords = terms
            .Where(term => normalizedMessage.Contains(term.NormalizedPhrase, StringComparison.Ordinal))
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
        @"\b(?<value>[А-ЯA-Z]{1,8}\d+[А-ЯA-Z0-9\-]*)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        RegexTimeoutMilliseconds)]
    private static partial Regex SeriesTokenRegex();

    [GeneratedRegex(
        @"[А-ЯA-Z0-9\-]+",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        RegexTimeoutMilliseconds)]
    private static partial Regex WordRegex();
}