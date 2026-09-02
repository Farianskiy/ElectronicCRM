using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Recognition.Management.UpdateProfile;

public sealed class UpdateCatalogCharacteristicRecognitionProfileCommandHandler
{
    private readonly CatalogRecognitionProfilePermissionChecker _permissionChecker;
    private readonly ICatalogCharacteristicRecognitionProfileRepository _profileRepository;

    public UpdateCatalogCharacteristicRecognitionProfileCommandHandler(
        CatalogRecognitionProfilePermissionChecker permissionChecker,
        ICatalogCharacteristicRecognitionProfileRepository profileRepository)
    {
        ArgumentNullException.ThrowIfNull(permissionChecker);
        ArgumentNullException.ThrowIfNull(profileRepository);

        _permissionChecker = permissionChecker;
        _profileRepository = profileRepository;
    }

    public async Task<UnitResult<DomainError>> Handle(
        UpdateCatalogCharacteristicRecognitionProfileCommand command,
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

        if (!Enum.TryParse<CatalogCharacteristicRecognitionStrategyKind>(
                command.StrategyKind,
                ignoreCase: true,
                out var strategyKind))
        {
            return UnitResult.Failure(
                GeneralErrors.ValueIsInvalid(nameof(command.StrategyKind)));
        }

        var profile = await _profileRepository
            .GetByIdAsync(command.ProfileId, cancellationToken)
            .ConfigureAwait(false);

        if (profile is null)
        {
            return UnitResult.Failure(
                CatalogRecognitionErrors.ProfileNotFound(command.ProfileId));
        }

        var configurationResult = CatalogRecognitionProfileConfigurationValidator.Validate(
            strategyKind,
            command.ConfigurationJson);

        if (configurationResult.IsFailure)
        {
            return configurationResult;
        }

        var duplicateProfileExists = await _profileRepository
            .ExistsAsync(
                profile.ProductTypeId,
                profile.CharacteristicDefinitionId,
                strategyKind,
                excludedProfileId: profile.Id,
                cancellationToken)
            .ConfigureAwait(false);

        if (duplicateProfileExists)
        {
            return UnitResult.Failure(
                CatalogRecognitionErrors.ProfileAlreadyExists(
                    profile.ProductTypeId,
                    profile.CharacteristicDefinitionId,
                    strategyKind));
        }

        var reconfigureResult = profile.Reconfigure(
            strategyKind,
            command.Priority,
            command.MinimumConfidence,
            command.ConfigurationJson);

        if (reconfigureResult.IsFailure)
        {
            return reconfigureResult;
        }

        await _profileRepository
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return UnitResult.Success<DomainError>();
    }
}