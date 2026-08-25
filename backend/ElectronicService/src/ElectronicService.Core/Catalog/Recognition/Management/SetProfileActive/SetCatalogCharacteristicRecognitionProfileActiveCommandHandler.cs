using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Recognition.Management.SetProfileActive;

public sealed class SetCatalogCharacteristicRecognitionProfileActiveCommandHandler
{
    private readonly CatalogRecognitionProfilePermissionChecker _permissionChecker;
    private readonly ICatalogCharacteristicRecognitionProfileRepository _profileRepository;

    public SetCatalogCharacteristicRecognitionProfileActiveCommandHandler(
        CatalogRecognitionProfilePermissionChecker permissionChecker,
        ICatalogCharacteristicRecognitionProfileRepository profileRepository)
    {
        ArgumentNullException.ThrowIfNull(permissionChecker);
        ArgumentNullException.ThrowIfNull(profileRepository);

        _permissionChecker = permissionChecker;
        _profileRepository = profileRepository;
    }

    public async Task<UnitResult<DomainError>> Handle(
        SetCatalogCharacteristicRecognitionProfileActiveCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var permissionResult = await _permissionChecker
            .EnsureCanManageAsync(cancellationToken)
            .ConfigureAwait(false);

        if (permissionResult.IsFailure)
        {
            return permissionResult;
        }

        if (command.ProfileId == Guid.Empty)
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(nameof(command.ProfileId)));
        }

        var profile = await _profileRepository
            .GetByIdAsync(command.ProfileId, cancellationToken)
            .ConfigureAwait(false);

        if (profile is null)
        {
            return UnitResult.Failure(
                CatalogRecognitionErrors.ProfileNotFound(command.ProfileId));
        }

        if (command.IsActive)
        {
            profile.Activate();
        }
        else
        {
            profile.Deactivate();
        }

        await _profileRepository
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return UnitResult.Success<DomainError>();
    }
}