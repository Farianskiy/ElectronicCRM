using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.Dictionaries.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.Dictionaries;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Dictionaries.SetTermActive;

public sealed class SetCatalogDictionaryTermActiveCommandHandler
{
    private readonly ICatalogDictionaryRepository _dictionaryRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserProvider _currentUserProvider;

    public SetCatalogDictionaryTermActiveCommandHandler(
        ICatalogDictionaryRepository dictionaryRepository,
        IUserRepository userRepository,
        ICurrentUserProvider currentUserProvider)
    {
        ArgumentNullException.ThrowIfNull(dictionaryRepository);
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(currentUserProvider);

        _dictionaryRepository = dictionaryRepository;
        _userRepository = userRepository;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<UnitResult<DomainError>> Handle(SetCatalogDictionaryTermActiveCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var currentUserId = _currentUserProvider.UserId;

        if (!currentUserId.HasValue)
        {
            return UnitResult.Failure(CatalogErrors.CurrentUserIsRequired());
        }

        var user = await _userRepository.GetByIdAsync(currentUserId.Value, cancellationToken).ConfigureAwait(false);

        if (user is null || !user.CanManageProductSynonyms())
        {
            return UnitResult.Failure(CatalogErrors.OnlyTechnicalUserCanManageDictionaryTerms());
        }

        if (command.TermId == Guid.Empty)
        {
            return UnitResult.Failure(GeneralErrors.ValueIsInvalid(nameof(command.TermId)));
        }

        var term = await _dictionaryRepository.GetByIdAsync(command.TermId, cancellationToken).ConfigureAwait(false);

        if (term is null)
        {
            return UnitResult.Failure(CatalogDictionaryTermErrors.TermNotFound(command.TermId));
        }

        UnitResult<DomainError> stateChangeResult;

        if (command.IsActive)
        {
            stateChangeResult = term.Reactivate(currentUserId.Value);
        }
        else
        {
            stateChangeResult = term.Disable(command.Reason ?? string.Empty, currentUserId.Value);
        }

        if (stateChangeResult.IsFailure)
        {
            return stateChangeResult;
        }

        await _dictionaryRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return UnitResult.Success<DomainError>();
    }
}