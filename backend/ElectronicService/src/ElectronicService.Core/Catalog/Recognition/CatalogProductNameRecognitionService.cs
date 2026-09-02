using ElectronicService.Core.Catalog.Dictionaries.Abstractions;
using ElectronicService.Core.Catalog.Dictionaries.GetTerms;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Configuration;
using ElectronicService.Core.Catalog.Recognition.Models;
using ElectronicService.Core.Catalog.Recognition.Normalization;

namespace ElectronicService.Core.Catalog.Recognition;

public sealed class CatalogProductNameRecognitionService : ICatalogProductNameRecognitionService
{
    private const string CharacteristicDictionaryTermKind = "Characteristic";
    private const decimal DictionaryConfidence = 0.9800m;

    private readonly ICatalogCharacteristicRecognitionStrategy[] _strategies;
    private readonly ICatalogCharacteristicRecognitionProfileReader _recognitionProfileReader;
    private readonly Lazy<Task<IReadOnlyCollection<CatalogDictionaryTermResult>>> _approvedDictionaryTerms;

    public CatalogProductNameRecognitionService(
        IEnumerable<ICatalogCharacteristicRecognitionStrategy> strategies,
        ICatalogDictionaryReader dictionaryReader,
        ICatalogCharacteristicRecognitionProfileReader recognitionProfileReader)
    {
        ArgumentNullException.ThrowIfNull(strategies);
        ArgumentNullException.ThrowIfNull(dictionaryReader);
        ArgumentNullException.ThrowIfNull(recognitionProfileReader);

        _strategies = strategies
            .OrderByDescending(strategy => strategy.Priority)
            .ThenBy(strategy => strategy.CharacteristicCode, StringComparer.Ordinal)
            .ThenBy(strategy => strategy.StrategyKind)
            .ToArray();

        _recognitionProfileReader = recognitionProfileReader;

        _approvedDictionaryTerms = new Lazy<Task<IReadOnlyCollection<CatalogDictionaryTermResult>>>(
            () => dictionaryReader.GetApprovedTermsAsync());
    }

    public async Task<CatalogProductNameRecognitionResult> RecognizeAsync(
        CatalogProductNameRecognitionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ProductName);

        cancellationToken.ThrowIfCancellationRequested();

        HashSet<string>? allowedCharacteristicCodes = null;

        if (request.AllowedCharacteristicCodes is not null)
        {
            allowedCharacteristicCodes = request.AllowedCharacteristicCodes
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Select(CatalogRecognitionTextNormalizer.NormalizeCode)
                .ToHashSet(StringComparer.Ordinal);
        }

        var hasProductTypeScope =
            request.ProductTypeId.HasValue &&
            request.ProductTypeId.Value != Guid.Empty;

        Guid? effectiveProductTypeId = hasProductTypeScope
            ? request.ProductTypeId
            : null;

        IReadOnlyCollection<CatalogCharacteristicRecognitionProfileResult> recognitionProfiles = [];

        if (effectiveProductTypeId.HasValue)
        {
            recognitionProfiles = await _recognitionProfileReader
                .GetProfilesAsync(effectiveProductTypeId.Value, cancellationToken)
                .ConfigureAwait(false);
        }

        var ruleCandidates = RecognizeRuleCandidates(
            request.ProductName,
            allowedCharacteristicCodes,
            recognitionProfiles,
            hasProductTypeScope);

        var dictionaryCandidates = await RecognizeDictionaryTermsAsync(
                request.ProductName,
                effectiveProductTypeId,
                allowedCharacteristicCodes,
                cancellationToken)
            .ConfigureAwait(false);

        var candidates = ruleCandidates
            .Concat(dictionaryCandidates)
            .Select(candidate =>
            {
                var normalizedCharacteristicCode =
                    CatalogRecognitionTextNormalizer.NormalizeCode(
                        candidate.CharacteristicCode);

                return candidate with
                {
                    CharacteristicCode = normalizedCharacteristicCode,
                    NormalizedValue = CatalogRecognitionValueNormalizer.Normalize(
                        normalizedCharacteristicCode,
                        candidate.NormalizedValue)
                };
            })
            .ToArray();

        var characteristics = new List<CatalogRecognizedCharacteristic>();
        var conflicts = new List<CatalogRecognitionConflict>();

        foreach (var candidateGroup in candidates.GroupBy(
            candidate => candidate.CharacteristicCode,
            StringComparer.Ordinal))
        {
            var orderedCandidates = candidateGroup
                .OrderByDescending(candidate => GetSourcePrecedence(candidate.Source))
                .ThenByDescending(candidate => candidate.Confidence)
                .ThenByDescending(candidate => candidate.Priority)
                .ThenBy(candidate => candidate.StartIndex)
                .ToArray();

            var highestSourcePrecedence =
                GetSourcePrecedence(orderedCandidates[0].Source);

            var authoritativeCandidates = orderedCandidates
                .Where(candidate =>
                    GetSourcePrecedence(candidate.Source) ==
                    highestSourcePrecedence)
                .ToArray();

            var distinctAuthoritativeValues = authoritativeCandidates
                .Select(candidate => candidate.NormalizedValue)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (distinctAuthoritativeValues.Length == 1)
            {
                characteristics.Add(authoritativeCandidates[0]);
                continue;
            }

            conflicts.Add(
                new CatalogRecognitionConflict(
                    candidateGroup.Key,
                    authoritativeCandidates));
        }

        return new CatalogProductNameRecognitionResult(
            request.ProductName,
            CatalogRecognitionTextNormalizer.NormalizeText(
                request.ProductName),
            characteristics
                .OrderBy(characteristic => characteristic.StartIndex)
                .ThenBy(
                    characteristic => characteristic.CharacteristicCode,
                    StringComparer.Ordinal)
                .ToArray(),
            conflicts
                .OrderBy(
                    conflict => conflict.CharacteristicCode,
                    StringComparer.Ordinal)
                .ToArray(),
            candidates
                .OrderBy(
                    candidate => candidate.CharacteristicCode,
                    StringComparer.Ordinal)
                .ThenByDescending(
                    candidate => GetSourcePrecedence(candidate.Source))
                .ThenByDescending(candidate => candidate.Confidence)
                .ThenBy(candidate => candidate.StartIndex)
                .ToArray());
    }

    private List<CatalogRecognizedCharacteristic> RecognizeRuleCandidates(
        string productName,
        HashSet<string>? allowedCharacteristicCodes,
        IReadOnlyCollection<CatalogCharacteristicRecognitionProfileResult> recognitionProfiles,
        bool hasProductTypeScope)
    {
        var candidates = new List<CatalogRecognizedCharacteristic>();

        foreach (var strategy in _strategies)
        {
            var normalizedCharacteristicCode =
                CatalogRecognitionTextNormalizer.NormalizeCode(
                    strategy.CharacteristicCode);

            if (allowedCharacteristicCodes is not null &&
                !allowedCharacteristicCodes.Contains(
                    normalizedCharacteristicCode))
            {
                continue;
            }

            CatalogCharacteristicRecognitionProfileResult? matchingProfile = null;

            if (hasProductTypeScope)
            {
                matchingProfile = recognitionProfiles
                    .Where(profile => string.Equals(
                        CatalogRecognitionTextNormalizer.NormalizeCode(
                            profile.CharacteristicCode),
                        normalizedCharacteristicCode,
                        StringComparison.Ordinal))
                    .Where(profile =>
                        profile.StrategyKind == strategy.StrategyKind)
                    .OrderByDescending(profile => profile.Priority)
                    .ThenBy(profile => profile.Id)
                    .FirstOrDefault();
            }

            if (matchingProfile is not null && !matchingProfile.IsActive)
            {
                continue;
            }

            IReadOnlyCollection<CatalogRecognizedCharacteristic> strategyCandidates;

            if (matchingProfile is not null && strategy is INumericWithUnitRecognitionStrategy numericWithUnitStrategy)
            {
                var settings = NumericWithUnitRecognitionSettingsParser.Parse(matchingProfile.ConfigurationJson);

                if (settings is null)
                {
                    continue;
                }

                strategyCandidates = numericWithUnitStrategy.Recognize(productName, settings);
            }
            else if (matchingProfile is not null && strategy is IBooleanAliasRecognitionStrategy booleanAliasStrategy)
            {
                var settings = BooleanAliasRecognitionSettingsParser.Parse(matchingProfile.ConfigurationJson);

                if (settings is null)
                {
                    continue;
                }

                strategyCandidates = booleanAliasStrategy.Recognize(productName, settings);
            }
            else
            {
                strategyCandidates = strategy.Recognize(productName);
            }

            if (matchingProfile is null)
            {
                candidates.AddRange(strategyCandidates);
                continue;
            }

            var configuredCandidates = strategyCandidates
                .Where(candidate =>
                    candidate.Confidence >=
                    matchingProfile.MinimumConfidence)
                .Select(candidate => candidate with
                {
                    Priority = matchingProfile.Priority,
                    RecognizerKey =
                        $"{candidate.RecognizerKey}:profile:{matchingProfile.Id}"
                });

            candidates.AddRange(configuredCandidates);
        }

        return candidates;
    }

    private async Task<IReadOnlyCollection<CatalogRecognizedCharacteristic>> RecognizeDictionaryTermsAsync(
        string productName,
        Guid? productTypeId,
        IReadOnlySet<string>? allowedCharacteristicCodes,
        CancellationToken cancellationToken)
    {
        var terms = await _approvedDictionaryTerms.Value
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        var normalizedProductName =
            CatalogRecognitionTextNormalizer.NormalizeText(productName);

        return terms
            .Where(term => string.Equals(
                term.Kind,
                CharacteristicDictionaryTermKind,
                StringComparison.Ordinal))
            .Where(term => !string.IsNullOrWhiteSpace(term.TargetCode))
            .Where(term => IsDictionaryTermAvailableForProductType(
                term,
                productTypeId))
            .Select(term => CreateDictionaryCandidate(
                term,
                normalizedProductName,
                allowedCharacteristicCodes))
            .Where(candidate => candidate is not null)
            .Cast<DictionaryCandidate>()
            .GroupBy(
                candidate =>
                    candidate.Characteristic.CharacteristicCode,
                StringComparer.Ordinal)
            .SelectMany(candidateGroup =>
                SelectDictionaryCandidates(
                    candidateGroup,
                    productTypeId))
            .Select(candidate => candidate.Characteristic)
            .ToArray();
    }

    private static bool IsDictionaryTermAvailableForProductType(
        CatalogDictionaryTermResult term,
        Guid? productTypeId)
    {
        if (!term.ProductTypeId.HasValue)
        {
            return true;
        }

        return productTypeId.HasValue &&
               term.ProductTypeId.Value == productTypeId.Value;
    }

    private static DictionaryCandidate? CreateDictionaryCandidate(
        CatalogDictionaryTermResult term,
        string normalizedProductName,
        IReadOnlySet<string>? allowedCharacteristicCodes)
    {
        var characteristicCode =
            CatalogRecognitionTextNormalizer.NormalizeCode(
                term.TargetCode!);

        if (allowedCharacteristicCodes is not null &&
            !allowedCharacteristicCodes.Contains(characteristicCode))
        {
            return null;
        }

        var normalizedPhrase =
            CatalogRecognitionTextNormalizer.NormalizeText(
                term.NormalizedPhrase);

        var startIndex = FindWholeTerm(
            normalizedProductName,
            normalizedPhrase);

        if (startIndex < 0)
        {
            return null;
        }

        var scopeKey = term.ProductTypeId.HasValue
            ? $"product-type:{term.ProductTypeId.Value}"
            : "global";

        return new DictionaryCandidate(
            new CatalogRecognizedCharacteristic(
                characteristicCode,
                term.Phrase,
                CatalogRecognitionTextNormalizer.NormalizeValue(
                    term.TargetValue),
                DictionaryConfidence,
                CatalogRecognitionSource.Dictionary,
                startIndex,
                normalizedPhrase.Length,
                term.Priority,
                $"dictionary:{term.Id}:scope:{scopeKey}"),
            normalizedPhrase.Length,
            term.ProductTypeId);
    }

    private static IEnumerable<DictionaryCandidate> SelectDictionaryCandidates(
        IGrouping<string, DictionaryCandidate> candidates,
        Guid? productTypeId)
    {
        var candidateArray = candidates.ToArray();

        var scopedCandidates = candidateArray
            .Where(candidate =>
                productTypeId.HasValue &&
                candidate.ProductTypeId.HasValue &&
                candidate.ProductTypeId.Value == productTypeId.Value)
            .ToArray();

        var candidatesInEffectiveScope = scopedCandidates.Length > 0
            ? scopedCandidates
            : candidateArray
                .Where(candidate => !candidate.ProductTypeId.HasValue)
                .ToArray();

        if (candidatesInEffectiveScope.Length == 0)
        {
            return [];
        }

        var maximumPhraseLength = candidatesInEffectiveScope
            .Max(candidate => candidate.PhraseLength);

        var longestCandidates = candidatesInEffectiveScope
            .Where(candidate =>
                candidate.PhraseLength == maximumPhraseLength)
            .ToArray();

        var maximumPriority = longestCandidates
            .Max(candidate =>
                candidate.Characteristic.Priority);

        return longestCandidates
            .Where(candidate =>
                candidate.Characteristic.Priority == maximumPriority)
            .GroupBy(
                candidate =>
                    candidate.Characteristic.NormalizedValue,
                StringComparer.Ordinal)
            .Select(group => group
                .OrderBy(candidate =>
                    candidate.Characteristic.StartIndex)
                .First());
    }

    private static int GetSourcePrecedence(
        CatalogRecognitionSource source)
    {
        return source switch
        {
            CatalogRecognitionSource.Dictionary => 400,
            CatalogRecognitionSource.Rule => 300,
            CatalogRecognitionSource.Heuristic => 200,
            CatalogRecognitionSource.MachineLearning => 100,
            CatalogRecognitionSource.None => 0,
            _ => 0
        };
    }

    private static int FindWholeTerm(
        string text,
        string term)
    {
        var searchStartIndex = 0;

        while (searchStartIndex <= text.Length - term.Length)
        {
            var matchIndex = text.IndexOf(
                term,
                searchStartIndex,
                StringComparison.Ordinal);

            if (matchIndex < 0)
            {
                return -1;
            }

            var hasStartBoundary =
                matchIndex == 0 ||
                !char.IsLetterOrDigit(text[matchIndex - 1]);

            var matchEndIndex =
                matchIndex + term.Length;

            var hasEndBoundary =
                matchEndIndex == text.Length ||
                !char.IsLetterOrDigit(text[matchEndIndex]);

            if (hasStartBoundary && hasEndBoundary)
            {
                return matchIndex;
            }

            searchStartIndex = matchIndex + 1;
        }

        return -1;
    }

    private sealed record DictionaryCandidate(
        CatalogRecognizedCharacteristic Characteristic,
        int PhraseLength,
        Guid? ProductTypeId);
}