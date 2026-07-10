using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.Abstractions;
using ElectronicService.Core.Catalog.Dictionaries.Abstractions;
using ElectronicService.Domain.Catalog.Dictionaries;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Common;
using ElectronicService.Core.Users;
using ElectronicService.Core.Abstractions;

namespace ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.ApproveSuggestion;

public sealed class ApproveCatalogAssistantDictionarySuggestionCommandHandler
{
    private readonly ICatalogAssistantDictionarySuggestionRepository _suggestionRepository;
    private readonly ICatalogDictionaryRepository _dictionaryRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserProvider _currentUserProvider;

    public ApproveCatalogAssistantDictionarySuggestionCommandHandler(
        ICatalogAssistantDictionarySuggestionRepository suggestionRepository,
        ICatalogDictionaryRepository dictionaryRepository,
        IUserRepository userRepository,
        ICurrentUserProvider currentUserProvider)
    {
        _suggestionRepository = suggestionRepository;
        _dictionaryRepository = dictionaryRepository;
        _userRepository = userRepository;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<UnitResult<DomainError>> Handle(
        ApproveCatalogAssistantDictionarySuggestionCommand command,
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

        var termResult = CatalogDictionaryTerm.Create(
            suggestion.UnknownPhrase,
            suggestion.SuggestedKind,
            suggestion.SuggestedTargetCode,
            suggestion.SuggestedTargetValue,
            100,
            CatalogDictionaryTermStatus.Approved,
            CatalogDictionaryTermSource.UserCorrection);

        if (termResult.IsFailure)
        {
            return termResult.Error;
        }

        var term = termResult.Value;

        var termAlreadyExists = await _dictionaryRepository
            .ExistsAsync(term, cancellationToken)
            .ConfigureAwait(false);

        if (!termAlreadyExists)
        {
            _dictionaryRepository.Add(term);
        }

        var approveResult = suggestion.Approve(
            currentUserId.Value,
            command.ReviewComment);

        if (approveResult.IsFailure)
        {
            return approveResult.Error;
        }

        await _suggestionRepository
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return UnitResult.Success<DomainError>();
    }
}