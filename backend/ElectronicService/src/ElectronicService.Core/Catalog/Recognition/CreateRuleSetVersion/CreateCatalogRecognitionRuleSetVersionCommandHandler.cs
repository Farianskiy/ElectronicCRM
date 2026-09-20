using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Core.Users;
using ElectronicService.Core.Users.Access;
using ElectronicService.Domain.Common;
using ElectronicService.Domain.Users.Enums;

namespace ElectronicService.Core.Catalog.Recognition.CreateRuleSetVersion;

public sealed class CreateCatalogRecognitionRuleSetVersionCommandHandler
{
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IUserRepository _userRepository;
    private readonly IUserPermissionOverrideRepository _permissionRepository;
    private readonly ICatalogProductMetadataRepository _metadataRepository;
    private readonly ICatalogRecognitionRuleSetVersionRepository _versionRepository;

    public CreateCatalogRecognitionRuleSetVersionCommandHandler(
        ICurrentUserProvider currentUserProvider,
        IUserRepository userRepository,
        IUserPermissionOverrideRepository permissionRepository,
        ICatalogProductMetadataRepository metadataRepository,
        ICatalogRecognitionRuleSetVersionRepository versionRepository)
    {
        _currentUserProvider = currentUserProvider;
        _userRepository = userRepository;
        _permissionRepository = permissionRepository;
        _metadataRepository = metadataRepository;
        _versionRepository = versionRepository;
    }

    public async Task<Result<CatalogRecognitionRuleSetVersionSaveResult, DomainError>> Handle(
        CreateCatalogRecognitionRuleSetVersionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (_currentUserProvider.UserId is not Guid userId || userId == Guid.Empty)
        {
            return new DomainError("training.unauthorized", "Необходимо войти в систему.");
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);

        if (user is null || !user.IsActive)
        {
            return new DomainError("training.forbidden", "Учётная запись недоступна.");
        }

        var overrides = await _permissionRepository.GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);
        var permissionOverride = overrides.SingleOrDefault(item => item.PermissionCode == UserPermissionCode.DictionariesManage);
        var hasPermission = permissionOverride?.IsAllowed ?? UserPermissionCatalog.GetDefaults(user.Type).Contains(UserPermissionCode.DictionariesManage);

        if (!hasPermission)
        {
            return new DomainError("training.forbidden", "Требуется право управления справочниками.");
        }

        if (command.ManufacturerId == Guid.Empty || command.ProductTypeId == Guid.Empty)
        {
            return new DomainError("training.invalid_request", "Укажите производителя и тип товара.");
        }

        if (string.IsNullOrWhiteSpace(command.Name) || command.Name.Length > 200)
        {
            return new DomainError("training.invalid_request", "Название версии должно содержать от 1 до 200 символов.");
        }

        if (command.Entries is null || command.Entries.Count == 0 || command.Entries.Count > 100)
        {
            return new DomainError("training.invalid_request", "Выберите от 1 до 100 шаблонов.");
        }

        var manufacturer = await _metadataRepository.GetManufacturerByIdAsync(
            command.ManufacturerId,
            cancellationToken).ConfigureAwait(false);

        var productType = await _metadataRepository.GetProductTypeByIdAsync(
            command.ProductTypeId,
            cancellationToken).ConfigureAwait(false);

        if (manufacturer is null || productType is null)
        {
            return new DomainError("training.not_found", "Производитель или тип товара не найден.");
        }

        var data = new CatalogRecognitionRuleSetVersionSaveData(
            command.ManufacturerId,
            command.ProductTypeId,
            command.Name,
            userId,
            command.Entries);

        return await _versionRepository.CreateAsync(data, cancellationToken).ConfigureAwait(false);
    }
}