using System.Text;

namespace ElectronicService.Core.Catalog.Products.Analogies;

public sealed record CatalogProductAnalogyCharacteristicProposal(
    string CharacteristicCode,
    string ProposedValue,
    int SupportingProductCount,
    int ProductsWithCharacteristicCount,
    decimal SupportRatio,
    bool IsSuggested,
    IReadOnlyCollection<string> BlockingReasons,
    IReadOnlyCollection<Guid> SupportingProductIds,
    IReadOnlyDictionary<string, int> AlternativeValues,
    Guid? FeedbackId = null,
    CatalogProductAnalogyMode Mode = CatalogProductAnalogyMode.Suggest,
    bool IsAutoFillAllowed = false,
    string? AutoFillBlockingReason = null);

public sealed record CatalogProductAnalogyProductTypeProposal(
    Guid ProductTypeId,
    string ProductTypeCode,
    string ProductTypeName,
    int SupportingProductCount,
    int EligibleProductCount,
    decimal SupportRatio,
    bool IsSuggested,
    IReadOnlyCollection<string> BlockingReasons,
    IReadOnlyCollection<Guid> SupportingProductIds);

public static class CatalogProductAnalogyProposalEvaluator
{
    public const int MinimumSupportingProductCount = 3;
    public const decimal MinimumSupportRatio = 0.80m;

    public static IReadOnlyCollection<CatalogProductAnalogyCharacteristicProposal>
        Evaluate(
            CatalogProductAnalogyRequest request,
            IReadOnlyCollection<CatalogProductAnalogyMatch> matches,
            CatalogProductAnalogySettings? settings = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(matches);

        if (!request.ProductTypeId.HasValue)
        {
            return [];
        }

        settings ??= CatalogProductAnalogySettings.SystemDefault;

        var knownCodes = request.KnownCharacteristics.Keys
            .Select(NormalizeIdentifier)
            .Where(code => code.Length > 0)
            .ToHashSet(StringComparer.Ordinal);

        var eligibleMatches = matches
            .Where(IsEligibleEvidence)
            .ToArray();

        var observations = eligibleMatches
            .SelectMany(match => match.Characteristics.Select(characteristic =>
                new CharacteristicObservation(
                    NormalizeIdentifier(characteristic.Key),
                    NormalizeValue(characteristic.Value),
                    characteristic.Value.Trim(),
                    match.ProductId)))
            .Where(observation =>
                observation.Code.Length > 0
                && observation.NormalizedValue.Length > 0
                && !knownCodes.Contains(observation.Code))
            .GroupBy(observation => observation.Code, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal);

        var proposals = new List<CatalogProductAnalogyCharacteristicProposal>();

        foreach (var characteristicGroup in observations)
        {
            var characteristicSettings = settings.Resolve(characteristicGroup.Key);
            if (characteristicSettings.Mode == CatalogProductAnalogyMode.Off)
            {
                continue;
            }

            var productObservations = characteristicGroup
                .GroupBy(observation => observation.ProductId)
                .Select(group => group
                    .OrderBy(observation => observation.NormalizedValue, StringComparer.Ordinal)
                    .ThenBy(observation => observation.DisplayValue, StringComparer.Ordinal)
                    .First())
                .ToArray();

            var valueGroups = productObservations
                .GroupBy(observation => observation.NormalizedValue, StringComparer.Ordinal)
                .Select(group => new ValueEvidence(
                    group.Key,
                    group
                        .GroupBy(observation => observation.DisplayValue, StringComparer.Ordinal)
                        .OrderByDescending(displayGroup => displayGroup.Count())
                        .ThenBy(displayGroup => displayGroup.Key, StringComparer.Ordinal)
                        .First()
                        .Key,
                    group
                        .Select(observation => observation.ProductId)
                        .Distinct()
                        .OrderBy(productId => productId)
                        .ToArray()))
                .OrderByDescending(evidence => evidence.ProductIds.Count)
                .ThenBy(evidence => evidence.NormalizedValue, StringComparer.Ordinal)
                .ToArray();

            var winningValue = valueGroups[0];
            var productsWithCharacteristicCount = productObservations.Length;
            var supportRatio = decimal.Divide(
                winningValue.ProductIds.Count,
                productsWithCharacteristicCount);

            var blockingReasons = new List<string>();
            var minimumSupportingProducts = characteristicSettings.MinimumSupportingProducts
                ?? settings.MinimumSupportingProducts;
            var minimumSupportRatio = characteristicSettings.MinimumSupportRatio
                ?? settings.MinimumSupportRatio;

            if (productsWithCharacteristicCount < settings.MinimumProductsWithCharacteristic)
            {
                blockingReasons.Add(
                    $"Нужно минимум {settings.MinimumProductsWithCharacteristic} товаров с этой характеристикой; найдено {productsWithCharacteristicCount}.");
            }

            if (winningValue.ProductIds.Count < minimumSupportingProducts)
            {
                blockingReasons.Add(
                    $"Нужно минимум {minimumSupportingProducts} подтверждающих товара; найдено {winningValue.ProductIds.Count}.");
            }

            if (supportRatio < minimumSupportRatio)
            {
                blockingReasons.Add(
                    $"Согласие аналогов должно быть не ниже {minimumSupportRatio:P0}; получено {supportRatio:P0}.");
            }

            if (valueGroups.Length > 1
                && valueGroups[1].ProductIds.Count == winningValue.ProductIds.Count)
            {
                blockingReasons.Add(
                    "Несколько значений имеют одинаковое число подтверждений.");
            }

            if (characteristicSettings.Mode == CatalogProductAnalogyMode.Shadow)
            {
                blockingReasons.Add("Режим Shadow: результат рассчитывается только для наблюдения.");
            }

            var isVariantCharacteristic = IsVariantCharacteristic(
                characteristicGroup.Key);
            var isAutoFillAllowed = characteristicSettings.Mode
                    == CatalogProductAnalogyMode.AutoFill
                && !isVariantCharacteristic
                && blockingReasons.Count == 0;
            var autoFillBlockingReason = isVariantCharacteristic
                && characteristicSettings.Mode == CatalogProductAnalogyMode.AutoFill
                    ? "Вариантная характеристика не заполняется по аналогам автоматически."
                    : null;

            proposals.Add(
                new CatalogProductAnalogyCharacteristicProposal(
                    characteristicGroup.Key,
                    winningValue.DisplayValue,
                    winningValue.ProductIds.Count,
                    productsWithCharacteristicCount,
                    supportRatio,
                    blockingReasons.Count == 0,
                    blockingReasons,
                    winningValue.ProductIds,
                    valueGroups.Take(settings.MaximumAlternativeValues).ToDictionary(
                        evidence => evidence.DisplayValue,
                        evidence => evidence.ProductIds.Count,
                        StringComparer.Ordinal),
                    Mode: characteristicSettings.Mode,
                    IsAutoFillAllowed: isAutoFillAllowed,
                    AutoFillBlockingReason: autoFillBlockingReason));
        }

        return proposals
            .OrderByDescending(proposal => proposal.IsSuggested)
            .ThenByDescending(proposal => proposal.SupportRatio)
            .ThenByDescending(proposal => proposal.SupportingProductCount)
            .ThenBy(proposal => proposal.CharacteristicCode, StringComparer.Ordinal)
            .ToArray();
    }

    public static IReadOnlyCollection<CatalogProductAnalogyProductTypeProposal>
        EvaluateProductTypes(
            CatalogProductAnalogyRequest request,
            IReadOnlyCollection<CatalogProductAnalogyMatch> matches,
            CatalogProductAnalogySettings? settings = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(matches);

        settings ??= CatalogProductAnalogySettings.SystemDefault;

        if (request.ProductTypeId.HasValue)
        {
            return [];
        }

        var eligibleMatches = matches
            .Where(IsEligibleEvidence)
            .GroupBy(match => match.ProductId)
            .Select(group => group.First())
            .ToArray();

        if (eligibleMatches.Length == 0)
        {
            return [];
        }

        var groupedTypes = eligibleMatches
            .GroupBy(match => new
            {
                match.ProductTypeId,
                match.ProductTypeCode,
                match.ProductTypeName
            })
            .Select(group => new
            {
                group.Key.ProductTypeId,
                group.Key.ProductTypeCode,
                group.Key.ProductTypeName,
                ProductIds = group
                    .Select(match => match.ProductId)
                    .Distinct()
                    .OrderBy(productId => productId)
                    .ToArray()
            })
            .OrderByDescending(group => group.ProductIds.Length)
            .ThenBy(group => group.ProductTypeName, StringComparer.Ordinal)
            .ThenBy(group => group.ProductTypeId)
            .ToArray();

        var highestSupportCount = groupedTypes[0].ProductIds.Length;
        var highestSupportIsTied = groupedTypes.Length > 1
            && groupedTypes[1].ProductIds.Length == highestSupportCount;

        return groupedTypes
            .Select(group =>
            {
                var supportRatio = decimal.Divide(
                    group.ProductIds.Length,
                    eligibleMatches.Length);

                var blockingReasons = new List<string>();

                if (group.ProductIds.Length < settings.MinimumSupportingProducts)
                {
                    blockingReasons.Add(
                        $"Нужно минимум {settings.MinimumSupportingProducts} подтверждающих товара; найдено {group.ProductIds.Length}.");
                }

                if (supportRatio < settings.MinimumSupportRatio)
                {
                    blockingReasons.Add(
                        $"Согласие аналогов должно быть не ниже {settings.MinimumSupportRatio:P0}; получено {supportRatio:P0}.");
                }

                if (highestSupportIsTied
                    && group.ProductIds.Length == highestSupportCount)
                {
                    blockingReasons.Add(
                        "Несколько типов имеют одинаковое число подтверждений.");
                }

                return new CatalogProductAnalogyProductTypeProposal(
                    group.ProductTypeId,
                    group.ProductTypeCode,
                    group.ProductTypeName,
                    group.ProductIds.Length,
                    eligibleMatches.Length,
                    supportRatio,
                    blockingReasons.Count == 0,
                    blockingReasons,
                    group.ProductIds);
            })
            .OrderByDescending(proposal => proposal.IsSuggested)
            .ThenByDescending(proposal => proposal.SupportRatio)
            .ThenBy(proposal => proposal.ProductTypeName, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool IsEligibleEvidence(CatalogProductAnalogyMatch match)
    {
        if (match.ConflictingCharacteristicCodes.Any(code =>
                !IsVariantCharacteristic(code)))
        {
            return false;
        }

        if (match.ExactArticleMatch || match.ExactNameMatch)
        {
            return true;
        }

        if (match.MatchedFamilyAnchor is not null
            && match.QueryNameTokenCoverage >= 0.20m)
        {
            return true;
        }

        return match.MatchingCharacteristicCount >= 2
            && match.SharedNameTokenCount >= 1;
    }

    private static bool IsVariantCharacteristic(string code)
    {
        return NormalizeIdentifier(code) is
            "RATEDCURRENT"
            or "RATED_CURRENT"
            or "NOMINALCURRENT"
            or "NOMINAL_CURRENT"
            or "DIAPASONSRABATVANIA"
            or "DIAPASON_SRABATVANIA"
            or "TRIPRANGE"
            or "POLES"
            or "POLECOUNT"
            or "POLE_COUNT"
            or "LEAKAGECURRENT"
            or "LEAKAGE_CURRENT"
            or "RESIDUALCURRENT"
            or "RESIDUAL_CURRENT"
            or "BREAKINGCAPACITY"
            or "BREAKING_CAPACITY";
    }

    private static string NormalizeIdentifier(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = new StringBuilder(value.Length);

        foreach (var character in value
            .Trim()
            .Where(character =>
                char.IsLetterOrDigit(character)
                || character == '_'))
        {
            normalized.Append(char.ToUpperInvariant(character));
        }

        return normalized.ToString();
    }

    private static string NormalizeValue(string? value)
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

            if (!previousWasSeparator && normalized.Length > 0)
            {
                normalized.Append(' ');
                previousWasSeparator = true;
            }
        }

        if (normalized.Length > 0 && normalized[^1] == ' ')
        {
            normalized.Length--;
        }

        return normalized.ToString();
    }

    private sealed record CharacteristicObservation(
        string Code,
        string NormalizedValue,
        string DisplayValue,
        Guid ProductId);

    private sealed record ValueEvidence(
        string NormalizedValue,
        string DisplayValue,
        IReadOnlyCollection<Guid> ProductIds);
}
