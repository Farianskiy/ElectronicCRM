using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.Abstractions;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Common;
using ElectronicService.Core.Users;
using ElectronicService.Core.Abstractions;

namespace ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.RejectSuggestion;

public sealed class RejectCatalogAssistantDictionarySuggestionCommandHandler
{
    private readonly ICatalogAssistantDictionarySuggestionRepository _suggestionRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserProvider _currentUserProvider;

    public RejectCatalogAssistantDictionarySuggestionCommandHandler(
        ICatalogAssistantDictionarySuggestionRepository suggestionRepository,
        IUserRepository userRepository,
        ICurrentUserProvider currentUserProvider)
    {
        _suggestionRepository = suggestionRepository;
        _userRepository = userRepository;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<UnitResult<DomainError>> Handle(
        RejectCatalogAssistantDictionarySuggestionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var currentUserId = _currentUserProvider.UserId;

        if (!currentUserId.HasValue)
        {
            return UnitResult.Failure<DomainError>(
                CatalogErrors.CurrentUserIsRequired());
        }

        var user = await _userRepository
            .GetByIdAsync(currentUserId.Value, cancellationToken)
            .ConfigureAwait(false);

        if (user is null || !user.CanManageProductSynonyms())
        {
            return UnitResult.Failure<DomainError>(
                CatalogErrors.UserCannotReviewDictionarySuggestion());
        }

        var suggestion = await _suggestionRepository
            .GetByIdAsync(command.SuggestionId, cancellationToken)
            .ConfigureAwait(false);

        if (suggestion is null)
        {
            return CatalogErrors.DictionarySuggestionNotFound(command.SuggestionId.ToString());
        }

        var rejectResult = suggestion.Reject(
            currentUserId.Value,
            command.ReviewComment);

        if (rejectResult.IsFailure)
        {
            return rejectResult.Error;
        }

        await _suggestionRepository
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return UnitResult.Success<DomainError>();
    }
}