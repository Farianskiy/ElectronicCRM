using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.RejectSuggestion;

public sealed class RejectCatalogAssistantDictionarySuggestionCommandHandler
{
    private readonly ICatalogAssistantDictionarySuggestionRepository _suggestionRepository;
    private readonly ICatalogRecognitionCandidateRepository _candidateRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserProvider _currentUserProvider;

    public RejectCatalogAssistantDictionarySuggestionCommandHandler(
        ICatalogAssistantDictionarySuggestionRepository suggestionRepository,
        ICatalogRecognitionCandidateRepository candidateRepository,
        IUserRepository userRepository,
        ICurrentUserProvider currentUserProvider)
    {
        ArgumentNullException.ThrowIfNull(suggestionRepository);
        ArgumentNullException.ThrowIfNull(candidateRepository);
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(currentUserProvider);

        _suggestionRepository = suggestionRepository;
        _candidateRepository = candidateRepository;
        _userRepository = userRepository;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<UnitResult<DomainError>> Handle(RejectCatalogAssistantDictionarySuggestionCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var currentUserId = _currentUserProvider.UserId;

        if (!currentUserId.HasValue)
        {
            return UnitResult.Failure<DomainError>(CatalogErrors.CurrentUserIsRequired());
        }

        var user = await _userRepository.GetByIdAsync(currentUserId.Value, cancellationToken).ConfigureAwait(false);

        if (user is null || !user.CanManageProductSynonyms())
        {
            return UnitResult.Failure<DomainError>(CatalogErrors.UserCannotReviewDictionarySuggestion());
        }

        var suggestion = await _suggestionRepository.GetByIdAsync(command.SuggestionId, cancellationToken).ConfigureAwait(false);

        if (suggestion is null)
        {
            return CatalogErrors.DictionarySuggestionNotFound(command.SuggestionId.ToString());
        }

        CatalogRecognitionCandidate? candidate = null;

        if (suggestion.IsGeneratedFromRecognitionLearning)
        {
            candidate = await _candidateRepository.GetBySuggestionIdAsync(suggestion.Id, cancellationToken).ConfigureAwait(false);

            if (candidate is null)
            {
                return CatalogErrors.RecognitionCandidateForSuggestionNotFound(suggestion.Id);
            }
        }

        var rejectResult = suggestion.Reject(currentUserId.Value, command.ReviewComment);

        if (rejectResult.IsFailure)
        {
            return rejectResult.Error;
        }

        if (candidate is not null)
        {
            var markCandidateResult = candidate.MarkRejected();

            if (markCandidateResult.IsFailure)
            {
                return markCandidateResult.Error;
            }
        }

        await _suggestionRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return UnitResult.Success<DomainError>();
    }
}