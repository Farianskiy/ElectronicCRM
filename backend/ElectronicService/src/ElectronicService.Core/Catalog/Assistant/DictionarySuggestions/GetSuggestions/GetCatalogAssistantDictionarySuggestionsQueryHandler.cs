using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.Dictionaries;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Common;

// using ElectronicService.Core.Users; // поставь namespace своего IUserRepository

namespace ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.GetSuggestions;

public sealed class GetCatalogAssistantDictionarySuggestionsQueryHandler
{
    private readonly ICatalogAssistantDictionarySuggestionReader _suggestionReader;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserProvider _currentUserProvider;

    public GetCatalogAssistantDictionarySuggestionsQueryHandler(
        ICatalogAssistantDictionarySuggestionReader suggestionReader,
        IUserRepository userRepository,
        ICurrentUserProvider currentUserProvider)
    {
        _suggestionReader = suggestionReader;
        _userRepository = userRepository;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<Result<CatalogAssistantDictionarySuggestionsPageResult, DomainError>> Handle(
        GetCatalogAssistantDictionarySuggestionsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var currentUserId = _currentUserProvider.UserId;

        if (!currentUserId.HasValue)
        {
            return CatalogErrors.CurrentUserIsRequired();
        }

        var user = await _userRepository
            .GetByIdAsync(currentUserId.Value, cancellationToken)
            .ConfigureAwait(false);

        if (user is null || !user.CanManageProductSynonyms())
        {
            return CatalogErrors.UserCannotReviewDictionarySuggestion();
        }

        CatalogAssistantDictionarySuggestionStatus? status = null;

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!Enum.TryParse<CatalogAssistantDictionarySuggestionStatus>(
                    query.Status,
                    ignoreCase: true,
                    out var parsedStatus))
            {
                return GeneralErrors.ValueIsInvalid(nameof(query.Status));
            }

            status = parsedStatus;
        }

        return await _suggestionReader
            .GetSuggestionsAsync(
                status,
                query.Page,
                query.PageSize,
                cancellationToken)
            .ConfigureAwait(false);
    }
}