using ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.Abstractions;
using ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.GetSuggestions;
using ElectronicService.Domain.Catalog.Dictionaries;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Queries;

public sealed class CatalogAssistantDictionarySuggestionReader
    : ICatalogAssistantDictionarySuggestionReader
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private readonly ElectronicDbContext _dbContext;

    public CatalogAssistantDictionarySuggestionReader(ElectronicDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CatalogAssistantDictionarySuggestionsPageResult> GetSuggestionsAsync(
        CatalogAssistantDictionarySuggestionStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = Math.Max(page, 1);

        var normalizedPageSize = pageSize <= 0
            ? DefaultPageSize
            : Math.Clamp(pageSize, 1, MaxPageSize);

        var query = _dbContext.CatalogAssistantDictionarySuggestions
            .AsNoTracking()
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(suggestion => suggestion.Status == status.Value);
        }

        var totalCount = await query
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        var itemsData = await query
            .OrderByDescending(suggestion => suggestion.CreatedAtUtc)
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(suggestion => new CatalogAssistantDictionarySuggestionData(
                suggestion.Id,
                suggestion.OriginalMessage,
                suggestion.UnknownPhrase,
                suggestion.NormalizedUnknownPhrase,
                suggestion.SuggestedKind,
                suggestion.SuggestedTargetCode,
                suggestion.SuggestedTargetValue,
                suggestion.Confidence,
                suggestion.Status,
                suggestion.CreatedByUserId,
                suggestion.CreatedAtUtc,
                suggestion.ReviewedByUserId,
                suggestion.ReviewedAtUtc,
                suggestion.ReviewComment))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = itemsData
            .Select(MapToResult)
            .ToList();

        return new CatalogAssistantDictionarySuggestionsPageResult(
            items,
            normalizedPage,
            normalizedPageSize,
            totalCount);
    }

    private static CatalogAssistantDictionarySuggestionResult MapToResult(
        CatalogAssistantDictionarySuggestionData suggestion)
    {
        return new CatalogAssistantDictionarySuggestionResult(
            suggestion.Id,
            suggestion.OriginalMessage,
            suggestion.UnknownPhrase,
            suggestion.NormalizedUnknownPhrase,
            suggestion.SuggestedKind.ToString(),
            suggestion.SuggestedTargetCode,
            suggestion.SuggestedTargetValue,
            suggestion.Confidence,
            suggestion.Status.ToString(),
            suggestion.CreatedByUserId,
            suggestion.CreatedAtUtc,
            suggestion.ReviewedByUserId,
            suggestion.ReviewedAtUtc,
            suggestion.ReviewComment);
    }

    private sealed record CatalogAssistantDictionarySuggestionData(
        Guid Id,
        string OriginalMessage,
        string UnknownPhrase,
        string NormalizedUnknownPhrase,
        CatalogDictionaryTermKind SuggestedKind,
        string? SuggestedTargetCode,
        string SuggestedTargetValue,
        decimal Confidence,
        CatalogAssistantDictionarySuggestionStatus Status,
        Guid CreatedByUserId,
        DateTime CreatedAtUtc,
        Guid? ReviewedByUserId,
        DateTime? ReviewedAtUtc,
        string? ReviewComment);
}