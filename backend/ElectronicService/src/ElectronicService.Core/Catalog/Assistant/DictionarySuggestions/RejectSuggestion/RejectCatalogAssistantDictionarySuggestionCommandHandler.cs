using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.Abstractions;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Common;
using ElectronicService.Core.Users;

namespace ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.RejectSuggestion;

public sealed class RejectCatalogAssistantDictionarySuggestionCommandHandler
{
    private readonly ICatalogAssistantDictionarySuggestionRepository _suggestionRepository;
    private readonly IUserRepository _userRepository;

    public RejectCatalogAssistantDictionarySuggestionCommandHandler(
        ICatalogAssistantDictionarySuggestionRepository suggestionRepository,
        IUserRepository userRepository)
    {
        _suggestionRepository = suggestionRepository;
        _userRepository = userRepository;
    }

    public async Task<UnitResult<DomainError>> Handle(
        RejectCatalogAssistantDictionarySuggestionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var user = await _userRepository
            .GetByIdAsync(command.ReviewedByUserId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null || !user.CanManageProductSynonyms())
        {
            return CatalogErrors.UserCannotReviewDictionarySuggestion();
        }

        var suggestion = await _suggestionRepository
            .GetByIdAsync(command.SuggestionId, cancellationToken)
            .ConfigureAwait(false);

        if (suggestion is null)
        {
            return CatalogErrors.DictionarySuggestionNotFound(command.SuggestionId.ToString());
        }

        var rejectResult = suggestion.Reject(
            command.ReviewedByUserId,
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