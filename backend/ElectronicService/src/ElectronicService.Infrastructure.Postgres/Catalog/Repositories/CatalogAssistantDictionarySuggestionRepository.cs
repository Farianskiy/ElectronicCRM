using ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.Abstractions;
using ElectronicService.Domain.Catalog.Dictionaries;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Repositories;

public sealed class CatalogAssistantDictionarySuggestionRepository
    : ICatalogAssistantDictionarySuggestionRepository
{
    private readonly ElectronicDbContext _dbContext;

    public CatalogAssistantDictionarySuggestionRepository(ElectronicDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<CatalogAssistantDictionarySuggestion?> GetByIdAsync(
        Guid suggestionId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.CatalogAssistantDictionarySuggestions
            .FirstOrDefaultAsync(
                suggestion => suggestion.Id == suggestionId,
                cancellationToken);
    }

    public Task<bool> ExistsPendingAsync(
        CatalogAssistantDictionarySuggestion suggestion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(suggestion);

        return _dbContext.CatalogAssistantDictionarySuggestions
            .AsNoTracking()
            .AnyAsync(
                existingSuggestion =>
                    existingSuggestion.Status == CatalogAssistantDictionarySuggestionStatus.Pending
                    && existingSuggestion.NormalizedUnknownPhrase == suggestion.NormalizedUnknownPhrase
                    && existingSuggestion.SuggestedKind == suggestion.SuggestedKind
                    && existingSuggestion.SuggestedTargetCode == suggestion.SuggestedTargetCode
                    && existingSuggestion.SuggestedTargetValue == suggestion.SuggestedTargetValue,
                cancellationToken);
    }

    public void Add(CatalogAssistantDictionarySuggestion suggestion)
    {
        ArgumentNullException.ThrowIfNull(suggestion);

        _dbContext.CatalogAssistantDictionarySuggestions.Add(suggestion);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}