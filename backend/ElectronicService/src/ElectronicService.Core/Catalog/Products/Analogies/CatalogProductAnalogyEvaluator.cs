using System.Text;

namespace ElectronicService.Core.Catalog.Products.Analogies;

public sealed record CatalogProductAnalogyRequest(
    string ProductName,
    string? Article,
    Guid ManufacturerId,
    Guid? ProductTypeId,
    IReadOnlyDictionary<string, string> KnownCharacteristics,
    Guid? ExcludedProductId = null,
    int MaximumResults = 20);

public sealed record CatalogProductAnalogyCandidate(
    Guid ProductId,
    string Article,
    string Name,
    Guid ManufacturerId,
    Guid ProductTypeId,
    string ProductTypeCode,
    string ProductTypeName,
    IReadOnlyCollection<string> Aliases,
    IReadOnlyDictionary<string, string> Characteristics,
    bool IsArchived);

public sealed record CatalogProductAnalogyMatch(
    Guid ProductId,
    string Article,
    string Name,
    Guid ManufacturerId,
    Guid ProductTypeId,
    string ProductTypeCode,
    string ProductTypeName,
    string MatchedName,
    bool ExactArticleMatch,
    bool ExactNameMatch,
    int SharedNameTokenCount,
    decimal QueryNameTokenCoverage,
    int MatchingCharacteristicCount,
    int ConflictingCharacteristicCount,
    IReadOnlyCollection<string> SharedNameTokens,
    IReadOnlyCollection<string> MatchingCharacteristicCodes,
    IReadOnlyCollection<string> ConflictingCharacteristicCodes,
    IReadOnlyDictionary<string, string> Characteristics,
    string? MatchedFamilyAnchor = null,
    IReadOnlyCollection<string>? CandidateFamilyAnchors = null);

public sealed record CatalogProductAnalogyCandidateExclusion(
    Guid ProductId,
    string Article,
    string Name,
    string ReasonCode,
    string Reason,
    IReadOnlyCollection<string> CandidateFamilyAnchors,
    IReadOnlyCollection<string> SharedNameTokens,
    int MatchingCharacteristicCount,
    int ConflictingCharacteristicCount);

public sealed record CatalogProductAnalogyResult(
    int EvaluatedCandidatesCount,
    IReadOnlyCollection<CatalogProductAnalogyMatch> Matches,
    IReadOnlyCollection<CatalogProductAnalogyCharacteristicProposal> CharacteristicProposals,
    IReadOnlyCollection<CatalogProductAnalogyProductTypeProposal> ProductTypeProposals,
    string? CandidateSnapshotId = null,
    DateTime? CandidateSnapshotCreatedAtUtc = null,
    Guid? ConfigurationId = null,
    int? ConfigurationVersion = null,
    string ConfigurationName = "Системная конфигурация",
    string ConfigurationHash = "",
    bool IsSystemConfiguration = true,
    int ExcludedCandidatesCount = 0,
    bool ExclusionDiagnosticsTruncated = false,
    IReadOnlyCollection<CatalogProductAnalogyCandidateExclusion>? ExcludedCandidates = null);

public static class CatalogProductAnalogyEvaluator
{
    private const int DefaultMaximumResults = 20;
    private const int MaximumAllowedResults = 100;
    private const int MaximumExclusionDiagnostics = 20;

    public static CatalogProductAnalogyResult Evaluate(
        CatalogProductAnalogyRequest request,
        IReadOnlyCollection<CatalogProductAnalogyCandidate> candidates,
        CatalogProductAnalogySettings? settings = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(candidates);

        if (string.IsNullOrWhiteSpace(request.ProductName))
        {
            throw new ArgumentException(
                "Укажите наименование товара.",
                nameof(request));
        }

        if (request.ManufacturerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Укажите производителя.",
                nameof(request));
        }

        if (request.ProductTypeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Идентификатор типа товара не может быть пустым.",
                nameof(request));
        }

        settings ??= CatalogProductAnalogySettings.SystemDefault;

        var maximumResults = request.MaximumResults <= 0
            ? DefaultMaximumResults
            : Math.Clamp(
                request.MaximumResults,
                1,
                MaximumAllowedResults);

        var normalizedProductName = NormalizeText(request.ProductName);
        var requestNameTokens = Tokenize(normalizedProductName);
        var normalizedArticle = NormalizeIdentifier(request.Article);
        var normalizedKnownCharacteristics =
            NormalizeCharacteristics(request.KnownCharacteristics);
        var requestFamilyAnchors = CatalogProductFamilyAnchorExtractor.Extract(
            request.ProductName,
            request.Article);

        var scopeExclusions = candidates
            .Select(candidate => TryCreateScopeExclusion(request, candidate))
            .Where(exclusion => exclusion is not null)
            .Cast<CatalogProductAnalogyCandidateExclusion>()
            .OrderBy(exclusion => exclusion.Name, StringComparer.Ordinal)
            .ThenBy(exclusion => exclusion.Article, StringComparer.Ordinal)
            .ThenBy(exclusion => exclusion.ProductId)
            .ToArray();

        var eligibleCandidates = candidates
            .Where(candidate =>
                !candidate.IsArchived
                && candidate.ProductId != request.ExcludedProductId
                && candidate.ManufacturerId == request.ManufacturerId
                && (!request.ProductTypeId.HasValue
                    || candidate.ProductTypeId == request.ProductTypeId.Value))
            .ToArray();

        var evaluatedCandidates = eligibleCandidates
            .Select(candidate =>
                EvaluateCandidate(
                    candidate,
                    normalizedProductName,
                    normalizedArticle,
                    requestNameTokens,
                    normalizedKnownCharacteristics,
                    requestFamilyAnchors))
            .ToArray();

        var meaningfulMatches = evaluatedCandidates
            .Where(IsMeaningfulMatch)
            .OrderByDescending(match => match.ExactArticleMatch)
            .ThenByDescending(match => match.ExactNameMatch)
            .ThenBy(match => match.ConflictingCharacteristicCount)
            .ThenByDescending(match => match.MatchingCharacteristicCount)
            .ThenByDescending(match => match.QueryNameTokenCoverage)
            .ThenByDescending(match => match.SharedNameTokenCount)
            .ThenBy(match => match.Name, StringComparer.Ordinal)
            .ThenBy(match => match.Article, StringComparer.Ordinal)
            .ThenBy(match => match.ProductId)
            .ToArray();

        var matches = meaningfulMatches
            .Take(maximumResults)
            .ToArray();

        var evidenceExclusions = evaluatedCandidates
            .Where(match => !IsMeaningfulMatch(match))
            .Select(CreateEvidenceExclusion)
            .OrderBy(exclusion => exclusion.Name, StringComparer.Ordinal)
            .ThenBy(exclusion => exclusion.Article, StringComparer.Ordinal)
            .ThenBy(exclusion => exclusion.ProductId)
            .ToArray();

        var excludedCandidatesCount =
            scopeExclusions.Length + evidenceExclusions.Length;
        var exclusionDiagnostics = scopeExclusions
            .Concat(evidenceExclusions)
            .Take(MaximumExclusionDiagnostics)
            .ToArray();

        return new CatalogProductAnalogyResult(
            eligibleCandidates.Length,
            matches,
            CatalogProductAnalogyProposalEvaluator.Evaluate(request, matches, settings),
            CatalogProductAnalogyProposalEvaluator.EvaluateProductTypes(request, matches, settings),
            ExcludedCandidatesCount: excludedCandidatesCount,
            ExclusionDiagnosticsTruncated:
                excludedCandidatesCount > exclusionDiagnostics.Length,
            ExcludedCandidates: exclusionDiagnostics);
    }

    private static CatalogProductAnalogyCandidateExclusion? TryCreateScopeExclusion(
        CatalogProductAnalogyRequest request,
        CatalogProductAnalogyCandidate candidate)
    {
        var (code, reason) = candidate switch
        {
            { IsArchived: true } => (
                "archived",
                "Товар архивирован."),
            _ when candidate.ProductId == request.ExcludedProductId => (
                "current_product",
                "Текущий товар не может подтверждать сам себя."),
            _ when candidate.ManufacturerId != request.ManufacturerId => (
                "manufacturer_mismatch",
                "Производитель не совпадает с выбранной областью."),
            _ when request.ProductTypeId.HasValue
                && candidate.ProductTypeId != request.ProductTypeId.Value => (
                    "product_type_mismatch",
                    "Тип товара не совпадает с выбранной областью."),
            _ => (null, null)
        };

        if (code is null || reason is null)
        {
            return null;
        }

        return new CatalogProductAnalogyCandidateExclusion(
            candidate.ProductId,
            candidate.Article,
            candidate.Name,
            code,
            reason,
            CatalogProductFamilyAnchorExtractor.Extract(
                candidate.Name,
                candidate.Article),
            [],
            0,
            0);
    }

    private static CatalogProductAnalogyCandidateExclusion CreateEvidenceExclusion(
        CatalogProductAnalogyMatch match)
    {
        var reason = match.MatchingCharacteristicCount == 1
            ? "Совпала только одна известная характеристика, а семейство не подтверждено."
            : "Совпали только общие слова или номиналы; семейство и сильные признаки не подтверждены.";

        return new CatalogProductAnalogyCandidateExclusion(
            match.ProductId,
            match.Article,
            match.Name,
            "family_evidence_missing",
            reason,
            match.CandidateFamilyAnchors ?? [],
            match.SharedNameTokens,
            match.MatchingCharacteristicCount,
            match.ConflictingCharacteristicCount);
    }

    private static CatalogProductAnalogyMatch EvaluateCandidate(
        CatalogProductAnalogyCandidate candidate,
        string normalizedProductName,
        string normalizedArticle,
        IReadOnlySet<string> requestNameTokens,
        IReadOnlyDictionary<string, string> knownCharacteristics,
        IReadOnlyCollection<string> requestFamilyAnchors)
    {
        var candidateNames = new[] { candidate.Name }
            .Concat(candidate.Aliases)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .Select(value =>
                EvaluateName(
                    value,
                    normalizedProductName,
                    requestNameTokens))
            .OrderByDescending(result => result.ExactMatch)
            .ThenByDescending(result => result.QueryTokenCoverage)
            .ThenByDescending(result => result.SharedTokens.Count)
            .ThenBy(result => result.Value, StringComparer.Ordinal)
            .ToArray();

        var bestName = candidateNames.FirstOrDefault()
            ?? new NameEvaluation(
                candidate.Name,
                ExactMatch: false,
                QueryTokenCoverage: 0m,
                SharedTokens: []);

        var normalizedCandidateCharacteristics =
            NormalizeCharacteristics(candidate.Characteristics);

        var matchingCharacteristicCodes = new List<string>();
        var conflictingCharacteristicCodes = new List<string>();

        foreach (var knownCharacteristic in knownCharacteristics)
        {
            if (!normalizedCandidateCharacteristics.TryGetValue(
                    knownCharacteristic.Key,
                    out var candidateValue))
            {
                continue;
            }

            if (string.Equals(
                    candidateValue,
                    knownCharacteristic.Value,
                    StringComparison.Ordinal))
            {
                matchingCharacteristicCodes.Add(
                    knownCharacteristic.Key);

                continue;
            }

            conflictingCharacteristicCodes.Add(
                knownCharacteristic.Key);
        }

        matchingCharacteristicCodes.Sort(StringComparer.Ordinal);
        conflictingCharacteristicCodes.Sort(StringComparer.Ordinal);

        var candidateArticle = NormalizeIdentifier(candidate.Article);

        var candidateFamilyAnchors = CatalogProductFamilyAnchorExtractor.Extract(
            bestName.Value,
            candidate.Article);
        var matchedFamilyAnchor = requestFamilyAnchors
            .FirstOrDefault(candidateFamilyAnchors.Contains);

        var exactArticleMatch =
            normalizedArticle.Length > 0
            && candidateArticle.Length > 0
            && string.Equals(
                normalizedArticle,
                candidateArticle,
                StringComparison.Ordinal);

        return new CatalogProductAnalogyMatch(
            candidate.ProductId,
            candidate.Article,
            candidate.Name,
            candidate.ManufacturerId,
            candidate.ProductTypeId,
            candidate.ProductTypeCode,
            candidate.ProductTypeName,
            bestName.Value,
            exactArticleMatch,
            bestName.ExactMatch,
            bestName.SharedTokens.Count,
            bestName.QueryTokenCoverage,
            matchingCharacteristicCodes.Count,
            conflictingCharacteristicCodes.Count,
            bestName.SharedTokens,
            matchingCharacteristicCodes,
            conflictingCharacteristicCodes,
            candidate.Characteristics,
            matchedFamilyAnchor,
            candidateFamilyAnchors);
    }

    private static NameEvaluation EvaluateName(
        string value,
        string normalizedProductName,
        IReadOnlySet<string> requestNameTokens)
    {
        var normalizedCandidateName = NormalizeText(value);
        var candidateTokens = Tokenize(normalizedCandidateName);

        var sharedTokens = requestNameTokens
            .Where(candidateTokens.Contains)
            .OrderBy(token => token, StringComparer.Ordinal)
            .ToArray();

        var queryTokenCoverage = requestNameTokens.Count == 0
            ? 0m
            : decimal.Divide(
                sharedTokens.Length,
                requestNameTokens.Count);

        return new NameEvaluation(
            value,
            string.Equals(
                normalizedProductName,
                normalizedCandidateName,
                StringComparison.Ordinal),
            queryTokenCoverage,
            sharedTokens);
    }

    private static bool IsMeaningfulMatch(
        CatalogProductAnalogyMatch match)
    {
        if (match.ExactArticleMatch || match.ExactNameMatch)
        {
            return true;
        }

        if (match.MatchingCharacteristicCount > 0)
        {
            return match.MatchingCharacteristicCount >= 2
                || match.MatchedFamilyAnchor is not null;
        }

        return match.MatchedFamilyAnchor is not null;
    }

    private static Dictionary<string, string> NormalizeCharacteristics(
        IReadOnlyDictionary<string, string> characteristics)
    {
        var result = new Dictionary<string, string>(
            StringComparer.Ordinal);

        foreach (var characteristic in characteristics)
        {
            var normalizedCode = NormalizeIdentifier(
                characteristic.Key);

            var normalizedValue = NormalizeText(
                characteristic.Value);

            if (normalizedCode.Length == 0
                || normalizedValue.Length == 0)
            {
                continue;
            }

            result[normalizedCode] = normalizedValue;
        }

        return result;
    }

    private static HashSet<string> Tokenize(string value)
    {
        var tokens = new HashSet<string>(
            StringComparer.Ordinal);

        var token = new StringBuilder();

        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(character))
            {
                token.Append(character);
                continue;
            }

            AddToken(tokens, token);
        }

        AddToken(tokens, token);

        return tokens;
    }

    private static void AddToken(
        HashSet<string> tokens,
        StringBuilder token)
    {
        if (token.Length == 0)
        {
            return;
        }

        tokens.Add(token.ToString());
        token.Clear();
    }

    private static string NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = new StringBuilder(value.Length);
        var previousWasSeparator = false;

        foreach (var sourceCharacter in value.Trim())
        {
            var character = sourceCharacter switch
            {
                'ё' or 'Ё' => 'Е',
                _ => char.ToUpperInvariant(sourceCharacter)
            };

            if (char.IsLetterOrDigit(character))
            {
                normalized.Append(character);
                previousWasSeparator = false;
                continue;
            }

            if (previousWasSeparator || normalized.Length == 0)
            {
                continue;
            }

            normalized.Append(' ');
            previousWasSeparator = true;
        }

        if (normalized.Length > 0
            && normalized[^1] == ' ')
        {
            normalized.Length--;
        }

        return normalized.ToString();
    }

    private static string NormalizeIdentifier(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = new StringBuilder(value.Length);

        foreach (var sourceCharacter in value)
        {
            var character = sourceCharacter switch
            {
                'ё' or 'Ё' => 'Е',
                _ => char.ToUpperInvariant(sourceCharacter)
            };

            if (char.IsLetterOrDigit(character))
            {
                normalized.Append(character);
            }
        }

        return normalized.ToString();
    }

    private sealed record NameEvaluation(
        string Value,
        bool ExactMatch,
        decimal QueryTokenCoverage,
        IReadOnlyCollection<string> SharedTokens);
}
