using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.ProductTypes.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Recognition.Management.CreateProfile;

public sealed class CreateCatalogCharacteristicRecognitionProfileCommandHandler
{
    private readonly CatalogRecognitionProfilePermissionChecker _permissionChecker;
    private readonly ICatalogProductTypeSchemaReader _productTypeSchemaReader;
    private readonly ICatalogCharacteristicRecognitionProfileRepository _profileRepository;

    public CreateCatalogCharacteristicRecognitionProfileCommandHandler(
        CatalogRecognitionProfilePermissionChecker permissionChecker,
        ICatalogProductTypeSchemaReader productTypeSchemaReader,
        ICatalogCharacteristicRecognitionProfileRepository profileRepository)
    {
        ArgumentNullException.ThrowIfNull(permissionChecker);
        ArgumentNullException.ThrowIfNull(productTypeSchemaReader);
        ArgumentNullException.ThrowIfNull(profileRepository);

        _permissionChecker = permissionChecker;
        _productTypeSchemaReader = productTypeSchemaReader;
        _profileRepository = profileRepository;
    }

    public async Task<Result<CreateCatalogCharacteristicRecognitionProfileResult, DomainError>> Handle(
        CreateCatalogCharacteristicRecognitionProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var permissionResult = await _permissionChecker
            .EnsureCanManageAsync(cancellationToken)
            .ConfigureAwait(false);

        if (permissionResult.IsFailure)
        {
            return permissionResult.Error;
        }

        if (string.IsNullOrWhiteSpace(command.ProductTypeCode))
        {
            return GeneralErrors.ValueIsRequired(nameof(command.ProductTypeCode));
        }

        if (command.CharacteristicDefinitionId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(command.CharacteristicDefinitionId));
        }

        if (!Enum.TryParse<CatalogCharacteristicRecognitionStrategyKind>(
                command.StrategyKind,
                ignoreCase: true,
                out var strategyKind))
        {
            return GeneralErrors.ValueIsInvalid(nameof(command.StrategyKind));
        }

        var productTypeSchema = await _productTypeSchemaReader
            .GetByCodeAsync(command.ProductTypeCode.Trim(), cancellationToken)
            .ConfigureAwait(false);

        if (productTypeSchema is null)
        {
            return CatalogErrors.ProductTypeNotFound(command.ProductTypeCode);
        }

        var characteristicIsAllowed = productTypeSchema.Characteristics.Any(
            characteristic => characteristic.DefinitionId == command.CharacteristicDefinitionId);

        if (!characteristicIsAllowed)
        {
            return CatalogErrors.CharacteristicIsNotAllowedForProductType(
                command.CharacteristicDefinitionId,
                productTypeSchema.ProductTypeId);
        }

        var configurationResult = CatalogRecognitionProfileConfigurationValidator.Validate(
            strategyKind,
            command.ConfigurationJson);

        if (configurationResult.IsFailure)
        {
            return configurationResult.Error;
        }

        var profileAlreadyExists = await _profileRepository
            .ExistsAsync(
                productTypeSchema.ProductTypeId,
                command.CharacteristicDefinitionId,
                strategyKind,
                excludedProfileId: null,
                cancellationToken)
            .ConfigureAwait(false);

        if (profileAlreadyExists)
        {
            return CatalogRecognitionErrors.ProfileAlreadyExists(
                productTypeSchema.ProductTypeId,
                command.CharacteristicDefinitionId,
                strategyKind);
        }

        var profileResult = CatalogCharacteristicRecognitionProfile.Create(
            productTypeSchema.ProductTypeId,
            command.CharacteristicDefinitionId,
            strategyKind,
            command.Priority,
            command.MinimumConfidence,
            command.ConfigurationJson);

        if (profileResult.IsFailure)
        {
            return profileResult.Error;
        }

        var profile = profileResult.Value;

        _profileRepository.Add(profile);

        await _profileRepository
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return new CreateCatalogCharacteristicRecognitionProfileResult(profile.Id);
    }
}