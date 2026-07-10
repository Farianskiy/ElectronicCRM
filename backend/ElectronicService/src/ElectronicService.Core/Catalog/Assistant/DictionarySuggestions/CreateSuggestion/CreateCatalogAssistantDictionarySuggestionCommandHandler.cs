using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.Dictionaries;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Common;
using ElectronicService.Core.Abstractions;

namespace ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.CreateSuggestion;

public sealed class CreateCatalogAssistantDictionarySuggestionCommandHandler
{
    private readonly ICatalogAssistantDictionarySuggestionRepository _suggestionRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserProvider _currentUserProvider;

    public CreateCatalogAssistantDictionarySuggestionCommandHandler(
        ICatalogAssistantDictionarySuggestionRepository suggestionRepository,
        IUserRepository userRepository,
        ICurrentUserProvider currentUserProvider)
    {
        _suggestionRepository = suggestionRepository;
        _userRepository = userRepository;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<Result<CreateCatalogAssistantDictionarySuggestionResult, DomainError>> Handle(
        CreateCatalogAssistantDictionarySuggestionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var currentUserId = _currentUserProvider.UserId;

        if (!currentUserId.HasValue)
        {
            return CatalogErrors.CurrentUserIsRequired();
        }

        var user = await _userRepository
            .GetByIdAsync(currentUserId.Value, cancellationToken)
            .ConfigureAwait(false);

        if (user is null || !user.CanUseAssistant())
        {
            return CatalogErrors.UserCannotCreateDictionarySuggestion();
        }

        if (!Enum.TryParse<CatalogDictionaryTermKind>(
                command.SuggestedKind,
                ignoreCase: true,
                out var suggestedKind))
        {
            return GeneralErrors.ValueIsInvalid(nameof(command.SuggestedKind));
        }

        var suggestionResult = CatalogAssistantDictionarySuggestion.Create(
            command.OriginalMessage,
            command.UnknownPhrase,
            suggestedKind,
            command.SuggestedTargetCode,
            command.SuggestedTargetValue,
            command.Confidence,
            currentUserId.Value);

        if (suggestionResult.IsFailure)
        {
            return suggestionResult.Error;
        }

        var suggestion = suggestionResult.Value;

        var alreadyExists = await _suggestionRepository
            .ExistsPendingAsync(suggestion, cancellationToken)
            .ConfigureAwait(false);

        if (alreadyExists)
        {
            return CatalogErrors.DictionarySuggestionAlreadyExists(command.UnknownPhrase);
        }

        _suggestionRepository.Add(suggestion);

        await _suggestionRepository
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return new CreateCatalogAssistantDictionarySuggestionResult(
            suggestion.Id,
            suggestion.Status.ToString());
    }
}