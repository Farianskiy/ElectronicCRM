using System.Text.RegularExpressions;
using ElectronicService.Core.Catalog.Assistant.Abstractions;
using ElectronicService.Core.Catalog.Assistant.AskCatalogAssistant;
using ElectronicService.Core.Catalog.Dictionaries.Abstractions;
using ElectronicService.Core.Catalog.Dictionaries.GetTerms;
using ElectronicService.Core.Catalog.Manufacturers.Resolution;
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
    private readonly IManufacturerResolver _manufacturerResolver;

    public RuleBasedCatalogAssistantMessageParser(
        ICatalogDictionaryReader dictionaryReader,
        ICatalogAssistantUnknownTermResolver unknownTermResolver,
        ICatalogProductNameRecognitionService productNameRecognitionService,
        IManufacturerResolver manufacturerResolver)
    {
        ArgumentNullException.ThrowIfNull(dictionaryReader);
        ArgumentNullException.ThrowIfNull(unknownTermResolver);
        ArgumentNullException.ThrowIfNull(productNameRecognitionService);
        ArgumentNullException.ThrowIfNull(manufacturerResolver);

        _dictionaryReader = dictionaryReader;
        _unknownTermResolver = unknownTermResolver;
        _productNameRecognitionService = productNameRecognitionService;
        _manufacturerResolver = manufacturerResolver;
    }

    public async Task<CatalogAssistantParsedRequest> ParseAsync(
        string message,
        string? selectedManufacturer,
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
            if (!Enum.TryParse<CatalogDictionaryTermKind>(term.Kind, ignoreCase: true, out var kind))
            {
                continue;
            }

            if (!IsDictionaryTermMatch(normalizedMessage, term.NormalizedPhrase, kind))
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

        var manufacturerResolutionIndex = await _manufacturerResolver
            .LoadIndexAsync(cancellationToken)
            .ConfigureAwait(false);

        var manufacturerRecognition = manufacturerResolutionIndex
            .RecognizeInText(message);

        var manufacturerSelection = ResolveManufacturerSelection(
            manufacturer,
            selectedManufacturer,
            manufacturerRecognition);

        manufacturer = manufacturerSelection.Manufacturer;

        search ??= ExtractSearchToken(
            message,
            recognitionResult);

        CatalogAssistantClarificationResult? clarification;

        if (manufacturerSelection.Clarification is not null)
        {
            clarification = manufacturerSelection.Clarification;
        }
        else
        {
            var unknownPhrase = FindFirstUnknownPhrase(
                normalizedMessage,
                terms,
                manufacturerRecognition);

            clarification = unknownPhrase is null
                ? null
                : await _unknownTermResolver
                    .ResolveAsync(unknownPhrase, cancellationToken)
                    .ConfigureAwait(false);
        }

        return new CatalogAssistantParsedRequest(
            intent,
            search,
            productTypeCode,
            manufacturer,
            characteristics,
            clarification,
            manufacturerRecognition);
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

    private static string? ExtractSearchToken(
    string message,
    CatalogProductNameRecognitionResult recognitionResult)
    {
        foreach (Match seriesMatch in SeriesTokenRegex().Matches(message))
        {
            var valueMatch = seriesMatch.Groups["value"];

            if (!valueMatch.Success)
            {
                continue;
            }

            var normalizedValue = NormalizeText(valueMatch.Value);

            if (normalizedValue.StartsWith("IP", StringComparison.Ordinal))
            {
                continue;
            }

            if (OverlapsRecognizedCharacteristic(
                    valueMatch.Index,
                    valueMatch.Length,
                    recognitionResult))
            {
                continue;
            }

            return normalizedValue;
        }

        return null;
    }

    private static bool OverlapsRecognizedCharacteristic(
        int startIndex,
        int length,
        CatalogProductNameRecognitionResult recognitionResult)
    {
        if (recognitionResult.Characteristics.Any(characteristic =>
                SpansOverlap(
                    startIndex,
                    length,
                    characteristic.StartIndex,
                    characteristic.Length)))
        {
            return true;
        }

        return recognitionResult.Conflicts.Any(conflict =>
            conflict.Candidates.Any(candidate =>
                SpansOverlap(
                    startIndex,
                    length,
                    candidate.StartIndex,
                    candidate.Length)));
    }

    private static bool SpansOverlap(
        int leftStartIndex,
        int leftLength,
        int rightStartIndex,
        int rightLength)
    {
        var leftEndIndex = leftStartIndex + leftLength;
        var rightEndIndex = rightStartIndex + rightLength;

        return leftStartIndex < rightEndIndex
            && rightStartIndex < leftEndIndex;
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

    private static string? FindFirstUnknownPhrase(
    string normalizedMessage,
    IReadOnlyCollection<CatalogDictionaryTermResult> terms,
    ManufacturerNameRecognitionResult manufacturerRecognition)
    {
        var recognizedWords = terms
            .Where(term =>
                Enum.TryParse<CatalogDictionaryTermKind>(
                    term.Kind,
                    ignoreCase: true,
                    out var kind)
                && IsDictionaryTermMatch(
                    normalizedMessage,
                    term.NormalizedPhrase,
                    kind))
            .SelectMany<CatalogDictionaryTermResult, string>(term =>
                term.NormalizedPhrase.Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var candidate in manufacturerRecognition.Candidates)
        {
            foreach (var recognizedWord in candidate.NormalizedValue.Split(
                         ' ',
                         StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                recognizedWords.Add(recognizedWord);
            }
        }

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

    private static (
    string? Manufacturer,
    CatalogAssistantClarificationResult? Clarification)
    ResolveManufacturerSelection(
        string? dictionaryManufacturer,
        string? selectedManufacturer,
        ManufacturerNameRecognitionResult manufacturerRecognition)
    {
        if (!string.IsNullOrWhiteSpace(selectedManufacturer))
        {
            var normalizedSelection = selectedManufacturer.Trim();

            var selectedCandidate = manufacturerRecognition.Candidates
                .FirstOrDefault(candidate =>
                    string.Equals(
                        candidate.ManufacturerName,
                        normalizedSelection,
                        StringComparison.OrdinalIgnoreCase));

            if (selectedCandidate is null)
            {
                return (
                    null,
                    CreateInvalidManufacturerSelectionClarification(
                        normalizedSelection,
                        manufacturerRecognition));
            }

            return (
                selectedCandidate.ManufacturerName,
                null);
        }

        if (manufacturerRecognition.IsConflict)
        {
            return (
                null,
                CreateManufacturerConflictClarification(
                    manufacturerRecognition));
        }

        if (!manufacturerRecognition.IsResolved)
        {
            return (
                dictionaryManufacturer,
                null);
        }

        var resolvedManufacturer = manufacturerRecognition
            .SelectedCandidate?
            .ManufacturerName
            ?? throw new InvalidOperationException(
                "Resolved manufacturer recognition does not contain a selected candidate.");

        return (
            resolvedManufacturer,
            null);
    }

    private static CatalogAssistantClarificationResult CreateManufacturerConflictClarification(
        ManufacturerNameRecognitionResult manufacturerRecognition)
    {
        var manufacturerNames = manufacturerRecognition.Candidates
            .Select(candidate => candidate.ManufacturerName)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(manufacturerName => manufacturerName, StringComparer.Ordinal)
            .ToArray();

        var rawValues = manufacturerRecognition.Candidates
            .OrderBy(candidate => candidate.StartIndex)
            .Select(candidate => candidate.RawValue)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var manufacturerNamesText = string.Join(", ", manufacturerNames);

        return new CatalogAssistantClarificationResult(
            string.Join(" / ", rawValues),
            "ManufacturerConflict",
            null,
            manufacturerNamesText,
            1.0000m,
            $"В запросе найдены разные производители: {manufacturerNamesText}. Уточните, какого производителя использовать.",
            false);
    }

    private static CatalogAssistantClarificationResult CreateInvalidManufacturerSelectionClarification(
    string selectedManufacturer,
    ManufacturerNameRecognitionResult manufacturerRecognition)
    {
        var availableManufacturers = manufacturerRecognition.Candidates
            .Select(candidate => candidate.ManufacturerName)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(manufacturerName => manufacturerName, StringComparer.Ordinal)
            .ToArray();

        var availableManufacturersText = availableManufacturers.Length == 0
            ? "нет доступных вариантов"
            : string.Join(", ", availableManufacturers);

        return new CatalogAssistantClarificationResult(
            selectedManufacturer,
            "ManufacturerSelectionInvalid",
            null,
            availableManufacturersText,
            0.0000m,
            $"Производитель {selectedManufacturer} не относится к найденным вариантам. Доступные варианты: {availableManufacturersText}.",
            false);
    }

    private static bool IsDictionaryTermMatch(
        string normalizedMessage,
        string normalizedPhrase,
        CatalogDictionaryTermKind kind)
    {
        return kind == CatalogDictionaryTermKind.Manufacturer
            ? ContainsWholeTerm(normalizedMessage, normalizedPhrase)
            : normalizedMessage.Contains(normalizedPhrase, StringComparison.Ordinal);
    }

    private static bool ContainsWholeTerm(string text, string term)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Length > text.Length)
        {
            return false;
        }

        var searchStartIndex = 0;

        while (searchStartIndex <= text.Length - term.Length)
        {
            var matchIndex = text.IndexOf(
                term,
                searchStartIndex,
                StringComparison.Ordinal);

            if (matchIndex < 0)
            {
                return false;
            }

            var hasStartBoundary =
                matchIndex == 0 ||
                !char.IsLetterOrDigit(text[matchIndex - 1]);

            var endIndex = matchIndex + term.Length;

            var hasEndBoundary =
                endIndex == text.Length ||
                !char.IsLetterOrDigit(text[endIndex]);

            if (hasStartBoundary && hasEndBoundary)
            {
                return true;
            }

            searchStartIndex = matchIndex + 1;
        }

        return false;
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
        @"\b(?<value>[А-ЯA-Z]{1,8}-?\d+[А-ЯA-Z0-9\-]*)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        RegexTimeoutMilliseconds)]
    private static partial Regex SeriesTokenRegex();

    [GeneratedRegex(
        @"[А-ЯA-Z0-9\-]+",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        RegexTimeoutMilliseconds)]
    private static partial Regex WordRegex();
}