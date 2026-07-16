using System.Globalization;
using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Core.Catalog.Products.GetReplacements;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Queries;

public sealed class CatalogProductReplacementsReader : ICatalogProductReplacementsReader
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private readonly ElectronicDbContext _dbContext;

    public CatalogProductReplacementsReader(ElectronicDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CatalogProductReplacementsResult?> GetReplacementsAsync(
        GetProductReplacementsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var normalizedPage = Math.Max(query.Page, 1);

        var normalizedPageSize = query.PageSize <= 0
            ? DefaultPageSize
            : Math.Clamp(query.PageSize, 1, MaxPageSize);

        var targetProduct = await (
            from product in _dbContext.Products.AsNoTracking()
            join productType in _dbContext.ProductTypes.AsNoTracking()
                on product.ProductTypeId equals productType.Id
            where product.Id == query.ProductId
            select new TargetProductSnapshot(
                product.Id,
                product.ProductTypeId,
                productType.Code,
                productType.Name))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (targetProduct is null)
        {
            return null;
        }

        var targetCharacteristics = await LoadCharacteristicsAsync(
            targetProduct.Id,
            cancellationToken)
            .ConfigureAwait(false);

        var replacementRules = await LoadReplacementRulesAsync(
            targetProduct.ProductTypeId,
            cancellationToken)
            .ConfigureAwait(false);

        var activeRules = replacementRules
            .Where(rule =>
                rule.Weight > 0
                && targetCharacteristics.ContainsKey(rule.CharacteristicDefinitionId))
            .ToList();

        if (activeRules.Count == 0)
        {
            return new CatalogProductReplacementsResult(
                targetProduct.Id,
                [],
                normalizedPage,
                normalizedPageSize,
                0);
        }

        var candidatesQuery =
            from product in _dbContext.Products.AsNoTracking()
            join manufacturer in _dbContext.Manufacturers.AsNoTracking()
                on product.ManufacturerId equals manufacturer.Id
            where product.ProductTypeId == targetProduct.ProductTypeId
                && product.Id != targetProduct.Id
                && (!query.OnlyInStock
                    || product.StockQuantity.Value > 0)
            select new CandidateProductSnapshot(
                product.Id,
                product.Article.Value,
                product.Name.Value,
                targetProduct.ProductTypeCode,
                targetProduct.ProductTypeName,
                manufacturer.Name,
                product.Price.Amount,
                product.Price.Currency,
                product.StockQuantity.Value);

        var candidates = await candidatesQuery
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (candidates.Count == 0)
        {
            return new CatalogProductReplacementsResult(
                targetProduct.Id,
                [],
                normalizedPage,
                normalizedPageSize,
                0);
        }

        var candidateIds = candidates
            .Select(candidate => candidate.Id)
            .ToList();

        var ruleCharacteristicIds = activeRules
            .Select(rule => rule.CharacteristicDefinitionId)
            .ToList();

        var candidateCharacteristics = await LoadCandidateCharacteristicsAsync(
            candidateIds,
            ruleCharacteristicIds,
            cancellationToken)
            .ConfigureAwait(false);

        var scoredCandidates = candidates
            .Select(candidate =>
            {
                candidateCharacteristics.TryGetValue(
                    candidate.Id,
                    out var characteristics);

                var score = CalculateReplacementScore(
                    targetCharacteristics,
                    characteristics ?? [],
                    activeRules);

                return new CatalogProductReplacementItemResult(
                    candidate.Id,
                    candidate.Article,
                    candidate.Name,
                    candidate.ProductTypeCode,
                    candidate.ProductTypeName,
                    candidate.ManufacturerName,
                    candidate.PriceAmount,
                    candidate.PriceCurrency,
                    candidate.StockQuantity,
                    score);
            })
            .Where(candidate => candidate.ReplacementScore >= query.MinimumScore)
            .OrderByDescending(candidate => candidate.ReplacementScore)
            .ThenBy(candidate => candidate.Name, StringComparer.Ordinal)
            .ToList();

        var totalCount = scoredCandidates.Count;

        var items = scoredCandidates
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToList();

        return new CatalogProductReplacementsResult(
            targetProduct.Id,
            items,
            normalizedPage,
            normalizedPageSize,
            totalCount);
    }

    private async Task<Dictionary<Guid, CharacteristicValueSnapshot>> LoadCharacteristicsAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var characteristics = await (
            from productCharacteristic in _dbContext.ProductCharacteristics.AsNoTracking()
            join definition in _dbContext.CharacteristicDefinitions.AsNoTracking()
                on productCharacteristic.CharacteristicDefinitionId equals definition.Id
            where productCharacteristic.ProductId == productId
            select new CharacteristicValueSnapshot(
                definition.Id,
                definition.DataType,
                productCharacteristic.Value.TextValue,
                productCharacteristic.Value.NumberValue,
                productCharacteristic.Value.BooleanValue))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return characteristics.ToDictionary(
            characteristic => characteristic.CharacteristicDefinitionId);
    }

    private async Task<List<ReplacementRuleSnapshot>> LoadReplacementRulesAsync(
        Guid productTypeId,
        CancellationToken cancellationToken)
    {
        return await (
            from productTypeCharacteristic in _dbContext.ProductTypeCharacteristics.AsNoTracking()
            join definition in _dbContext.CharacteristicDefinitions.AsNoTracking()
                on productTypeCharacteristic.CharacteristicDefinitionId equals definition.Id
            where productTypeCharacteristic.ProductTypeId == productTypeId
                  && productTypeCharacteristic.IsUsedForReplacement
                  && productTypeCharacteristic.ReplacementMatchMode != ReplacementMatchMode.None
            select new ReplacementRuleSnapshot(
                definition.Id,
                definition.DataType,
                definition.Code,
                productTypeCharacteristic.ReplacementMatchMode,
                productTypeCharacteristic.ReplacementWeight))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<Dictionary<Guid, Dictionary<Guid, CharacteristicValueSnapshot>>> LoadCandidateCharacteristicsAsync(
        IReadOnlyCollection<Guid> candidateIds,
        IReadOnlyCollection<Guid> characteristicDefinitionIds,
        CancellationToken cancellationToken)
    {
        var characteristics = await (
            from productCharacteristic in _dbContext.ProductCharacteristics.AsNoTracking()
            join definition in _dbContext.CharacteristicDefinitions.AsNoTracking()
                on productCharacteristic.CharacteristicDefinitionId equals definition.Id
            where candidateIds.Contains(productCharacteristic.ProductId)
                  && characteristicDefinitionIds.Contains(productCharacteristic.CharacteristicDefinitionId)
            select new
            {
                productCharacteristic.ProductId,
                Value = new CharacteristicValueSnapshot(
                    definition.Id,
                    definition.DataType,
                    productCharacteristic.Value.TextValue,
                    productCharacteristic.Value.NumberValue,
                    productCharacteristic.Value.BooleanValue)
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return characteristics
            .GroupBy(characteristic => characteristic.ProductId)
            .ToDictionary(
                group => group.Key,
                group => group.ToDictionary(
                    characteristic => characteristic.Value.CharacteristicDefinitionId,
                    characteristic => characteristic.Value));
    }

    private static decimal CalculateReplacementScore(
        Dictionary<Guid, CharacteristicValueSnapshot> targetCharacteristics,
        Dictionary<Guid, CharacteristicValueSnapshot> candidateCharacteristics,
        List<ReplacementRuleSnapshot> rules)
    {
        var maxScore = rules.Sum(rule => rule.Weight);

        if (maxScore <= 0)
        {
            return 0;
        }

        var score = 0m;

        foreach (var rule in rules)
        {
            if (!targetCharacteristics.TryGetValue(
                    rule.CharacteristicDefinitionId,
                    out var targetValue))
            {
                continue;
            }

            if (!candidateCharacteristics.TryGetValue(
                    rule.CharacteristicDefinitionId,
                    out var candidateValue))
            {
                continue;
            }

            score += CalculateRuleScore(rule, targetValue, candidateValue);
        }

        return Math.Round(score / maxScore * 100m, 2);
    }

    private static decimal CalculateRuleScore(
        ReplacementRuleSnapshot rule,
        CharacteristicValueSnapshot targetValue,
        CharacteristicValueSnapshot candidateValue)
    {
        return rule.MatchMode switch
        {
            ReplacementMatchMode.Exact => IsExactMatch(
                rule.DataType,
                targetValue,
                candidateValue)
                ? rule.Weight
                : 0,

            ReplacementMatchMode.GreaterOrEqual => CalculateGreaterOrEqualScore(
                rule,
                targetValue,
                candidateValue),

            ReplacementMatchMode.Near => CalculateNearScore(
                rule,
                targetValue,
                candidateValue),

            ReplacementMatchMode.CompatibleOrHigher => CalculateCompatibleOrHigherScore(
                rule,
                targetValue,
                candidateValue),

            _ => 0
        };
    }

    private static decimal CalculateGreaterOrEqualScore(
        ReplacementRuleSnapshot rule,
        CharacteristicValueSnapshot targetValue,
        CharacteristicValueSnapshot candidateValue)
    {
        if (rule.DataType != CharacteristicDataType.Number
            || targetValue.NumberValue is null
            || candidateValue.NumberValue is null)
        {
            return IsExactMatch(rule.DataType, targetValue, candidateValue)
                ? rule.Weight
                : 0;
        }

        return candidateValue.NumberValue.Value >= targetValue.NumberValue.Value
            ? rule.Weight
            : 0;
    }

    private static decimal CalculateNearScore(
        ReplacementRuleSnapshot rule,
        CharacteristicValueSnapshot targetValue,
        CharacteristicValueSnapshot candidateValue)
    {
        if (rule.DataType != CharacteristicDataType.Number
            || targetValue.NumberValue is null
            || candidateValue.NumberValue is null)
        {
            return IsExactMatch(rule.DataType, targetValue, candidateValue)
                ? rule.Weight
                : 0;
        }

        var targetNumber = targetValue.NumberValue.Value;
        var candidateNumber = candidateValue.NumberValue.Value;

        if (targetNumber == candidateNumber)
        {
            return rule.Weight;
        }

        if (targetNumber == 0)
        {
            return 0;
        }

        var differenceRatio = Math.Abs(candidateNumber - targetNumber) / Math.Abs(targetNumber);

        if (differenceRatio <= 0.10m)
        {
            return rule.Weight * 0.8m;
        }

        if (differenceRatio <= 0.20m)
        {
            return rule.Weight * 0.5m;
        }

        return 0;
    }

    private static decimal CalculateCompatibleOrHigherScore(
        ReplacementRuleSnapshot rule,
        CharacteristicValueSnapshot targetValue,
        CharacteristicValueSnapshot candidateValue)
    {
        if (rule.DataType == CharacteristicDataType.Text
            && TryParseIpRating(targetValue.TextValue, out var targetIp)
            && TryParseIpRating(candidateValue.TextValue, out var candidateIp))
        {
            return candidateIp.FirstDigit >= targetIp.FirstDigit
                   && candidateIp.SecondDigit >= targetIp.SecondDigit
                ? rule.Weight
                : 0;
        }

        return IsExactMatch(rule.DataType, targetValue, candidateValue)
            ? rule.Weight
            : 0;
    }

    private static bool IsExactMatch(
        CharacteristicDataType dataType,
        CharacteristicValueSnapshot targetValue,
        CharacteristicValueSnapshot candidateValue)
    {
        return dataType switch
        {
            CharacteristicDataType.Text => string.Equals(
                NormalizeText(targetValue.TextValue ?? string.Empty),
                NormalizeText(candidateValue.TextValue ?? string.Empty),
                StringComparison.Ordinal),

            CharacteristicDataType.Number => targetValue.NumberValue == candidateValue.NumberValue,

            CharacteristicDataType.Boolean => targetValue.BooleanValue == candidateValue.BooleanValue,

            _ => false
        };
    }

    private static bool TryParseIpRating(
        string? value,
        out IpRatingSnapshot ipRating)
    {
        ipRating = new IpRatingSnapshot(0, 0);

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalizedValue = NormalizeText(value);

        var digits = new string(
            normalizedValue
                .Where(char.IsDigit)
                .Take(2)
                .ToArray());

        if (digits.Length < 2)
        {
            return false;
        }

        ipRating = new IpRatingSnapshot(
            digits[0] - '0',
            digits[1] - '0');

        return true;
    }

    private static string NormalizeText(string value)
    {
        return value
            .Trim()
            .ToUpperInvariant()
            .Replace("Ё", "Е", StringComparison.Ordinal);
    }

    private sealed record TargetProductSnapshot(
        Guid Id,
        Guid ProductTypeId,
        string ProductTypeCode,
        string ProductTypeName);

    private sealed record CandidateProductSnapshot(
        Guid Id,
        string Article,
        string Name,
        string ProductTypeCode,
        string ProductTypeName,
        string ManufacturerName,
        decimal PriceAmount,
        string PriceCurrency,
        decimal StockQuantity);

    private sealed record ReplacementRuleSnapshot(
        Guid CharacteristicDefinitionId,
        CharacteristicDataType DataType,
        string Code,
        ReplacementMatchMode MatchMode,
        int Weight);

    private sealed record CharacteristicValueSnapshot(
        Guid CharacteristicDefinitionId,
        CharacteristicDataType DataType,
        string? TextValue,
        decimal? NumberValue,
        bool? BooleanValue);

    private sealed record IpRatingSnapshot(
        int FirstDigit,
        int SecondDigit);
}