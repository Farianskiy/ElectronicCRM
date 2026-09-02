using ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.Abstractions;
using ElectronicService.Domain.Catalog.Dictionaries;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Repositories;

public sealed class CatalogAssistantDictionarySuggestionRepository : ICatalogAssistantDictionarySuggestionRepository
{
    private readonly ElectronicDbContext _dbContext;

    public CatalogAssistantDictionarySuggestionRepository(ElectronicDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public Task<CatalogAssistantDictionarySuggestion?> GetByIdAsync(Guid suggestionId, CancellationToken cancellationToken = default)
    {
        return _dbContext.CatalogAssistantDictionarySuggestions.FirstOrDefaultAsync(
            suggestion => suggestion.Id == suggestionId,
            cancellationToken);
    }

    public async Task<CatalogAssistantDictionarySuggestion?> GetEquivalentPendingAsync(CatalogAssistantDictionarySuggestion suggestion, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(suggestion);

        var localSuggestion = _dbContext.CatalogAssistantDictionarySuggestions.Local.FirstOrDefault(
            existingSuggestion =>
                existingSuggestion.Status == CatalogAssistantDictionarySuggestionStatus.Pending
                && string.Equals(existingSuggestion.NormalizedUnknownPhrase, suggestion.NormalizedUnknownPhrase, StringComparison.Ordinal)
                && existingSuggestion.SuggestedKind == suggestion.SuggestedKind
                && string.Equals(existingSuggestion.SuggestedTargetCode, suggestion.SuggestedTargetCode, StringComparison.Ordinal)
                && string.Equals(existingSuggestion.SuggestedTargetValue, suggestion.SuggestedTargetValue, StringComparison.Ordinal)
                && existingSuggestion.ProductTypeId == suggestion.ProductTypeId
                && existingSuggestion.CharacteristicDefinitionId == suggestion.CharacteristicDefinitionId);

        if (localSuggestion is not null)
        {
            return localSuggestion;
        }

        return await _dbContext.CatalogAssistantDictionarySuggestions.FirstOrDefaultAsync(
            existingSuggestion =>
                existingSuggestion.Status == CatalogAssistantDictionarySuggestionStatus.Pending
                && existingSuggestion.NormalizedUnknownPhrase == suggestion.NormalizedUnknownPhrase
                && existingSuggestion.SuggestedKind == suggestion.SuggestedKind
                && existingSuggestion.SuggestedTargetCode == suggestion.SuggestedTargetCode
                && existingSuggestion.SuggestedTargetValue == suggestion.SuggestedTargetValue
                && existingSuggestion.ProductTypeId == suggestion.ProductTypeId
                && existingSuggestion.CharacteristicDefinitionId == suggestion.CharacteristicDefinitionId,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> ExistsPendingAsync(CatalogAssistantDictionarySuggestion suggestion, CancellationToken cancellationToken = default)
    {
        return await GetEquivalentPendingAsync(suggestion, cancellationToken).ConfigureAwait(false) is not null;
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