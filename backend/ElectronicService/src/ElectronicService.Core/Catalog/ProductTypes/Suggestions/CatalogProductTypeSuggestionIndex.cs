using ElectronicService.Core.Catalog.Dictionaries.GetTerms;
using ElectronicService.Core.Catalog.Metadata.GetProductTypes;

namespace ElectronicService.Core.Catalog.ProductTypes.Suggestions;

public sealed class CatalogProductTypeSuggestionIndex
{
    private const string ProductTypeTermKind = "ProductType";
    private const int StrongTermPriority = 100;
    private const decimal StrongTermConfidence = 0.9900m;
    private const decimal GenericTermConfidence = 0.9000m;

    private readonly IReadOnlyCollection<IndexedProductTypeTerm> _terms;

    public CatalogProductTypeSuggestionIndex(
        IReadOnlyCollection<CatalogProductTypeResult> productTypes,
        IReadOnlyCollection<CatalogDictionaryTermResult> dictionaryTerms)
    {
        ArgumentNullException.ThrowIfNull(productTypes);
        ArgumentNullException.ThrowIfNull(dictionaryTerms);

        var productTypesByCode = productTypes
            .ToDictionary(
                productType => NormalizeCode(productType.Code),
                productType => productType,
                StringComparer.Ordinal);

        var indexedTerms = new List<IndexedProductTypeTerm>();

        foreach (var term in dictionaryTerms)
        {
            if (!string.Equals(
                    term.Kind,
                    ProductTypeTermKind,
                    StringComparison.Ordinal))
            {
                continue;
            }

            var normalizedTargetCode = NormalizeCode(term.TargetValue);

            if (!productTypesByCode.TryGetValue(
                    normalizedTargetCode,
                    out var productType))
            {
                continue;
            }

            if (term.ProductTypeId.HasValue &&
                term.ProductTypeId.Value != productType.Id)
            {
                continue;
            }

            var normalizedPhrase =
                NormalizeTextPreservingLength(term.NormalizedPhrase);

            if (string.IsNullOrWhiteSpace(normalizedPhrase))
            {
                continue;
            }

            indexedTerms.Add(
                new IndexedProductTypeTerm(
                    term.Id,
                    term.Phrase,
                    normalizedPhrase,
                    term.Priority,
                    term.Source,
                    productType));
        }

        _terms = indexedTerms
            .OrderByDescending(term => term.Priority)
            .ThenByDescending(term => term.NormalizedPhrase.Length)
            .ThenBy(term => term.NormalizedPhrase, StringComparer.Ordinal)
            .ThenBy(term => term.DictionaryTermId)
            .ToArray();
    }

    public CatalogProductTypeSuggestionResult Suggest(
        string productName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productName);

        cancellationToken.ThrowIfCancellationRequested();

        var normalizedProductName =
            NormalizeTextPreservingLength(productName);

        var evidenceMatches =
            new List<ProductTypeEvidenceMatch>();

        foreach (var term in _terms)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var startIndex in FindWholeTermStartIndexes(
                         normalizedProductName,
                         term.NormalizedPhrase))
            {
                evidenceMatches.Add(
                    new ProductTypeEvidenceMatch(
                        term.ProductType,
                        new CatalogProductTypeSuggestionEvidence(
                            term.DictionaryTermId,
                            term.Phrase,
                            productName.Substring(
                                startIndex,
                                term.NormalizedPhrase.Length),
                            term.NormalizedPhrase,
                            term.Priority,
                            term.Source,
                            startIndex,
                            term.NormalizedPhrase.Length)));
            }
        }

        var candidates = evidenceMatches
            .GroupBy(match => match.ProductType.Id)
            .Select(CreateCandidate)
            .OrderByDescending(candidate => candidate.HighestPriority)
            .ThenByDescending(candidate =>
                candidate.Evidence.Max(evidence => evidence.Length))
            .ThenBy(candidate =>
                candidate.Evidence.Min(evidence => evidence.StartIndex))
            .ThenBy(
                candidate => candidate.ProductTypeCode,
                StringComparer.Ordinal)
            .ToArray();

        if (candidates.Length == 0)
        {
            return new CatalogProductTypeSuggestionResult(
                productName,
                normalizedProductName,
                CatalogProductTypeSuggestionStatus.Unresolved,
                null,
                candidates);
        }

        var highestPriority = candidates[0].HighestPriority;

        var authoritativeCandidates = candidates
            .Where(candidate =>
                candidate.HighestPriority == highestPriority)
            .ToArray();

        if (authoritativeCandidates.Length > 1)
        {
            return new CatalogProductTypeSuggestionResult(
                productName,
                normalizedProductName,
                CatalogProductTypeSuggestionStatus.Conflict,
                null,
                candidates);
        }

        return new CatalogProductTypeSuggestionResult(
            productName,
            normalizedProductName,
            CatalogProductTypeSuggestionStatus.Suggested,
            authoritativeCandidates[0],
            candidates);
    }

    private static CatalogProductTypeSuggestionCandidate CreateCandidate(
        IGrouping<Guid, ProductTypeEvidenceMatch> matches)
    {
        var matchArray = matches.ToArray();
        var productType = matchArray[0].ProductType;

        var evidence = matchArray
            .Select(match => match.Evidence)
            .OrderByDescending(item => item.Priority)
            .ThenByDescending(item => item.Length)
            .ThenBy(item => item.StartIndex)
            .ThenBy(item => item.DictionaryTermId)
            .ToArray();

        var highestPriority =
            evidence.Max(item => item.Priority);

        return new CatalogProductTypeSuggestionCandidate(
            productType.Id,
            productType.Code,
            productType.Name,
            highestPriority,
            GetConfidence(highestPriority),
            evidence);
    }

    private static decimal GetConfidence(int highestPriority)
    {
        return highestPriority >= StrongTermPriority
            ? StrongTermConfidence
            : GenericTermConfidence;
    }

    private static IEnumerable<int> FindWholeTermStartIndexes(
        string text,
        string term)
    {
        if (string.IsNullOrWhiteSpace(term) ||
            term.Length > text.Length)
        {
            yield break;
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
                yield break;
            }

            if (HasWholeTokenBoundaries(
                    text,
                    matchIndex,
                    term.Length))
            {
                yield return matchIndex;
            }

            searchStartIndex = matchIndex + 1;
        }
    }

    private static bool HasWholeTokenBoundaries(
        string text,
        int startIndex,
        int length)
    {
        var hasStartBoundary =
            startIndex == 0 ||
            !IsTokenCharacter(text[startIndex - 1]);

        var endIndex = startIndex + length;

        var hasEndBoundary =
            endIndex == text.Length ||
            !IsTokenCharacter(text[endIndex]);

        return hasStartBoundary && hasEndBoundary;
    }

    private static bool IsTokenCharacter(char character)
    {
        return char.IsLetterOrDigit(character);
    }

    private static string NormalizeTextPreservingLength(string value)
    {
        var normalizedCharacters = value.ToCharArray();

        for (var index = 0; index < normalizedCharacters.Length; index++)
        {
            normalizedCharacters[index] =
                normalizedCharacters[index] switch
                {
                    'ё' or 'Ё' => 'Е',
                    _ => char.ToUpperInvariant(
                        normalizedCharacters[index])
                };
        }

        return new string(normalizedCharacters);
    }

    private static string NormalizeCode(string value)
    {
        return value
            .Trim()
            .ToUpperInvariant();
    }

    private sealed record IndexedProductTypeTerm(
        Guid DictionaryTermId,
        string Phrase,
        string NormalizedPhrase,
        int Priority,
        string Source,
        CatalogProductTypeResult ProductType);

    private sealed record ProductTypeEvidenceMatch(
        CatalogProductTypeResult ProductType,
        CatalogProductTypeSuggestionEvidence Evidence);
}