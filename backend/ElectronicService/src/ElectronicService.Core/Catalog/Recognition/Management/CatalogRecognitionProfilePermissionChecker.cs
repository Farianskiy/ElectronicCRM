using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Recognition.Management;

public sealed class CatalogRecognitionProfilePermissionChecker
{
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IUserRepository _userRepository;

    public CatalogRecognitionProfilePermissionChecker(
        ICurrentUserProvider currentUserProvider,
        IUserRepository userRepository)
    {
        ArgumentNullException.ThrowIfNull(currentUserProvider);
        ArgumentNullException.ThrowIfNull(userRepository);

        _currentUserProvider = currentUserProvider;
        _userRepository = userRepository;
    }

    public async Task<UnitResult<DomainError>> EnsureCanManageAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserProvider.UserId;

        if (!currentUserId.HasValue)
        {
            return UnitResult.Failure(CatalogErrors.CurrentUserIsRequired());
        }

        var user = await _userRepository
            .GetByIdAsync(currentUserId.Value, cancellationToken)
            .ConfigureAwait(false);

        if (user is null || !user.CanManageCatalogRecognitionProfiles())
        {
            return UnitResult.Failure(CatalogRecognitionErrors.UserCannotManageProfiles());
        }

        return UnitResult.Success<DomainError>();
    }
}