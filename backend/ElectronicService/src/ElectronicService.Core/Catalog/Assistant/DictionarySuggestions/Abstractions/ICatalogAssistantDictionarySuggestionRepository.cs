using ElectronicService.Domain.Catalog.Dictionaries;

namespace ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.Abstractions;

public interface ICatalogAssistantDictionarySuggestionRepository
{
    Task<CatalogAssistantDictionarySuggestion?> GetByIdAsync(
        Guid suggestionId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsPendingAsync(
        CatalogAssistantDictionarySuggestion suggestion,
        CancellationToken cancellationToken = default);

    void Add(CatalogAssistantDictionarySuggestion suggestion);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}