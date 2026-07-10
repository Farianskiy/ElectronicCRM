using ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.GetSuggestions;
using ElectronicService.Domain.Catalog.Dictionaries;

namespace ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.Abstractions;

public interface ICatalogAssistantDictionarySuggestionReader
{
    Task<CatalogAssistantDictionarySuggestionsPageResult> GetSuggestionsAsync(
        CatalogAssistantDictionarySuggestionStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}