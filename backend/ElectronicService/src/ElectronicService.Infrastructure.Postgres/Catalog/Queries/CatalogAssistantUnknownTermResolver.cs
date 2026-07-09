using System.Globalization;
using System.Text;
using ElectronicService.Core.Catalog.Assistant.Abstractions;
using ElectronicService.Core.Catalog.Assistant.AskCatalogAssistant;
using ElectronicService.Domain.Catalog.Dictionaries;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Queries;

public sealed class CatalogAssistantUnknownTermResolver
    : ICatalogAssistantUnknownTermResolver
{
    private const decimal MinimumConfidence = 0.65m;
    private const int MaxSeriesCandidates = 500;

    private readonly ElectronicDbContext _dbContext;

    public CatalogAssistantUnknownTermResolver(ElectronicDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CatalogAssistantClarificationResult?> ResolveAsync(
        string unknownPhrase,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(unknownPhrase);

        var candidates = new List<CatalogAssistantSuggestionCandidate>();

        var manufacturerCandidates = await _dbContext.Manufacturers
            .AsNoTracking()
            .Select(manufacturer => new CatalogAssistantSuggestionCandidate(
                manufacturer.NormalizedName,
                CatalogDictionaryTermKind.Manufacturer,
                null,
                manufacturer.NormalizedName))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        candidates.AddRange(manufacturerCandidates);

        var productTypeCandidates = await _dbContext.ProductTypes
            .AsNoTracking()
            .Select(productType => new CatalogAssistantSuggestionCandidate(
                productType.Name,
                CatalogDictionaryTermKind.ProductType,
                null,
                productType.Code))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        candidates.AddRange(productTypeCandidates);

        var productSeriesDefinitionId = await _dbContext.CharacteristicDefinitions
            .AsNoTracking()
            .Where(definition => definition.Code == "PRODUCT_SERIES")
            .Select(definition => definition.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (productSeriesDefinitionId != Guid.Empty)
        {
            var seriesCandidates = await _dbContext.ProductCharacteristics
                .AsNoTracking()
                .Where(characteristic =>
                    characteristic.CharacteristicDefinitionId == productSeriesDefinitionId
                    && characteristic.Value.TextValue != null)
                .Select(characteristic => characteristic.Value.TextValue!)
                .Distinct()
                .Take(MaxSeriesCandidates)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            candidates.AddRange(seriesCandidates.Select(series => new CatalogAssistantSuggestionCandidate(
                series,
                CatalogDictionaryTermKind.Characteristic,
                "PRODUCT_SERIES",
                NormalizeText(series))));
        }

        var bestCandidate = candidates
            .Select(candidate => new
            {
                Candidate = candidate,
                Confidence = CalculateConfidence(unknownPhrase, candidate.Phrase)
            })
            .Where(candidate => candidate.Confidence >= MinimumConfidence)
            .OrderByDescending(candidate => candidate.Confidence)
            .FirstOrDefault();

        if (bestCandidate is null)
        {
            return null;
        }

        var question = CreateQuestion(
            unknownPhrase,
            bestCandidate.Candidate);

        return new CatalogAssistantClarificationResult(
            NormalizeText(unknownPhrase),
            bestCandidate.Candidate.Kind.ToString(),
            bestCandidate.Candidate.TargetCode,
            bestCandidate.Candidate.TargetValue,
            bestCandidate.Confidence,
            question);
    }

    private static string CreateQuestion(
        string unknownPhrase,
        CatalogAssistantSuggestionCandidate candidate)
    {
        var normalizedUnknownPhrase = NormalizeText(unknownPhrase);

        return candidate.Kind switch
        {
            CatalogDictionaryTermKind.Manufacturer =>
                $"Я не понял слово \"{normalizedUnknownPhrase}\". Возможно, вы имели в виду производителя {candidate.TargetValue}?",

            CatalogDictionaryTermKind.ProductType =>
                $"Я не понял слово \"{normalizedUnknownPhrase}\". Возможно, вы имели в виду тип товара {candidate.TargetValue}?",

            CatalogDictionaryTermKind.Characteristic =>
                $"Я не понял слово \"{normalizedUnknownPhrase}\". Возможно, это характеристика {candidate.TargetCode} = {candidate.TargetValue}?",

            _ =>
                $"Я не понял слово \"{normalizedUnknownPhrase}\"."
        };
    }

    private static decimal CalculateConfidence(
        string source,
        string candidate)
    {
        var normalizedSource = NormalizeForSimilarity(source);
        var normalizedCandidate = NormalizeForSimilarity(candidate);

        if (normalizedSource.Length == 0 || normalizedCandidate.Length == 0)
        {
            return 0;
        }

        if (normalizedCandidate.Contains(normalizedSource, StringComparison.Ordinal)
            || normalizedSource.Contains(normalizedCandidate, StringComparison.Ordinal))
        {
            return 0.85m;
        }

        var distance = CalculateLevenshteinDistance(
            normalizedSource,
            normalizedCandidate);

        var maxLength = Math.Max(
            normalizedSource.Length,
            normalizedCandidate.Length);

        return 1m - (decimal)distance / maxLength;
    }

    private static int CalculateLevenshteinDistance(
        string source,
        string target)
    {
        if (source.Length == 0)
        {
            return target.Length;
        }

        if (target.Length == 0)
        {
            return source.Length;
        }

        var previousRow = new int[target.Length + 1];
        var currentRow = new int[target.Length + 1];

        for (var index = 0; index <= target.Length; index++)
        {
            previousRow[index] = index;
        }

        for (var sourceIndex = 0; sourceIndex < source.Length; sourceIndex++)
        {
            currentRow[0] = sourceIndex + 1;

            for (var targetIndex = 0; targetIndex < target.Length; targetIndex++)
            {
                var insertCost = currentRow[targetIndex] + 1;
                var deleteCost = previousRow[targetIndex + 1] + 1;
                var replaceCost = previousRow[targetIndex]
                    + (source[sourceIndex] == target[targetIndex] ? 0 : 1);

                currentRow[targetIndex + 1] = Math.Min(
                    Math.Min(insertCost, deleteCost),
                    replaceCost);
            }

            (previousRow, currentRow) = (currentRow, previousRow);
        }

        return previousRow[target.Length];
    }

    private static string NormalizeForSimilarity(string value)
    {
        return Transliterate(NormalizeText(value))
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal);
    }

    private static string NormalizeText(string value)
    {
        return value
            .Trim()
            .ToUpperInvariant()
            .Replace("Ё", "Е", StringComparison.Ordinal);
    }

    private static string Transliterate(string value)
    {
        var builder = new StringBuilder(value.Length);

        foreach (var character in value)
        {
            builder.Append(character switch
            {
                'А' => "A",
                'Б' => "B",
                'В' => "V",
                'Г' => "G",
                'Д' => "D",
                'Е' => "E",
                'Ж' => "ZH",
                'З' => "Z",
                'И' => "I",
                'Й' => "I",
                'К' => "K",
                'Л' => "L",
                'М' => "M",
                'Н' => "N",
                'О' => "O",
                'П' => "P",
                'Р' => "R",
                'С' => "S",
                'Т' => "T",
                'У' => "U",
                'Ф' => "F",
                'Х' => "H",
                'Ц' => "C",
                'Ч' => "CH",
                'Ш' => "SH",
                'Щ' => "SCH",
                'Ы' => "Y",
                'Э' => "E",
                'Ю' => "YU",
                'Я' => "YA",
                'Ь' => string.Empty,
                'Ъ' => string.Empty,
                _ => character.ToString(CultureInfo.InvariantCulture)
            });
        }

        return builder.ToString();
    }

    private sealed record CatalogAssistantSuggestionCandidate(
        string Phrase,
        CatalogDictionaryTermKind Kind,
        string? TargetCode,
        string TargetValue);
}