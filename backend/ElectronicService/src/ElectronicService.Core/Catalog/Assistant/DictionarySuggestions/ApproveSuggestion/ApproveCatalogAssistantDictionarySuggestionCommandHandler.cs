using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.Abstractions;
using ElectronicService.Core.Catalog.Dictionaries.Abstractions;
using ElectronicService.Domain.Catalog.Dictionaries;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Common;
using ElectronicService.Core.Users;

namespace ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.ApproveSuggestion;

public sealed class ApproveCatalogAssistantDictionarySuggestionCommandHandler
{
    private readonly ICatalogAssistantDictionarySuggestionRepository _suggestionRepository;
    private readonly ICatalogDictionaryRepository _dictionaryRepository;
    private readonly IUserRepository _userRepository;

    public ApproveCatalogAssistantDictionarySuggestionCommandHandler(
        ICatalogAssistantDictionarySuggestionRepository suggestionRepository,
        ICatalogDictionaryRepository dictionaryRepository,
        IUserRepository userRepository)
    {
        _suggestionRepository = suggestionRepository;
        _dictionaryRepository = dictionaryRepository;
        _userRepository = userRepository;
    }

    public async Task<UnitResult<DomainError>> Handle(
        ApproveCatalogAssistantDictionarySuggestionCommand command,
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
            command.ReviewedByUserId,
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