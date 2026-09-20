using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Core.Users;
using ElectronicService.Core.Users.Access;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Domain.Common;
using ElectronicService.Domain.Users.Enums;

namespace ElectronicService.Core.Catalog.Recognition.SaveLiteralDraft;

public sealed class SaveCatalogRecognitionLiteralDraftCommandHandler
{
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IUserRepository _userRepository;
    private readonly IUserPermissionOverrideRepository _permissionRepository;
    private readonly ICatalogProductMetadataRepository _metadataRepository;
    private readonly CatalogRecognitionTrainingDataLoader _loader;
    private readonly ICatalogRecognitionLiteralDraftRepository _draftRepository;

    public SaveCatalogRecognitionLiteralDraftCommandHandler(
        ICurrentUserProvider currentUserProvider,
        IUserRepository userRepository,
        IUserPermissionOverrideRepository permissionRepository,
        ICatalogProductMetadataRepository metadataRepository,
        CatalogRecognitionTrainingDataLoader loader,
        ICatalogRecognitionLiteralDraftRepository draftRepository)
    {
        _currentUserProvider = currentUserProvider;
        _userRepository = userRepository;
        _permissionRepository = permissionRepository;
        _metadataRepository = metadataRepository;
        _loader = loader;
        _draftRepository = draftRepository;
    }

    public async Task<Result<Guid, DomainError>> Handle(SaveCatalogRecognitionLiteralDraftCommand command, CancellationToken cancellationToken = default)
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
        var permissionOverride = overrides.SingleOrDefault(value => value.PermissionCode == UserPermissionCode.DictionariesManage);
        var hasPermission = permissionOverride?.IsAllowed ?? UserPermissionCatalog.GetDefaults(user.Type).Contains(UserPermissionCode.DictionariesManage);

        if (!hasPermission)
        {
            return new DomainError("training.forbidden", "Требуется право управления справочниками.");
        }

        if (command.ManufacturerId == Guid.Empty || command.ProductTypeId == Guid.Empty || command.CharacteristicDefinitionId == Guid.Empty)
        {
            return new DomainError("training.invalid_request", "Укажите производителя, тип товара и характеристику.");
        }

        if (string.IsNullOrWhiteSpace(command.Literal) || command.Literal.Length > 2000 || string.IsNullOrWhiteSpace(command.NormalizedValue) || command.NormalizedValue.Length > 2000)
        {
            return new DomainError("training.invalid_request", "Фрагмент и значение должны быть заполнены и не превышать 2000 символов.");
        }

        if (!string.Equals(command.GeneratorVersion, CatalogRecognitionLiteralProposalGenerator.Version, StringComparison.Ordinal))
        {
            return new DomainError("training.conflict", "Версия генератора изменилась. Повторите предварительный просмотр.");
        }

        var manufacturer = await _metadataRepository.GetManufacturerByIdAsync(command.ManufacturerId, cancellationToken).ConfigureAwait(false);

        if (manufacturer is null)
        {
            return new DomainError("training.not_found", "Производитель не найден.");
        }

        var productType = await _metadataRepository.GetProductTypeByIdAsync(command.ProductTypeId, cancellationToken).ConfigureAwait(false);

        if (productType is null || !productType.Characteristics.Any(characteristic => characteristic.CharacteristicDefinitionId == command.CharacteristicDefinitionId))
        {
            return new DomainError("training.invalid_request", "Тип товара не найден или характеристика ему не принадлежит.");
        }

        var scope = new CatalogRecognitionTrainingScope(command.ManufacturerId, command.ProductTypeId, command.CharacteristicDefinitionId);
        var prepared = await _loader.LoadAsync(scope, cancellationToken).ConfigureAwait(false);

        if (!prepared.CanGenerate)
        {
            return new DomainError("training.conflict", "Учебные примеры не прошли подготовку. Повторите предварительный просмотр и устраните указанные проблемы.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var generated = CatalogRecognitionLiteralProposalGenerator.Generate(prepared);

        var proposal = generated.Proposals.SingleOrDefault(candidate => string.Equals(candidate.Literal, command.Literal, StringComparison.Ordinal) && string.Equals(candidate.NormalizedValue, command.NormalizedValue, StringComparison.Ordinal));

        if (proposal is null || !proposal.PassedExamples)
        {
            return new DomainError("training.conflict", "Выбранное предложение больше не проходит проверку на текущих примерах.");
        }

        var checkedIds = prepared.Samples.SelectMany(sample => sample.ExampleIds).Distinct().ToArray();

        var creationResult = CatalogRecognitionLiteralDraft.Create(
            command.ManufacturerId,
            command.ProductTypeId,
            command.CharacteristicDefinitionId,
            proposal.Literal,
            proposal.NormalizedValue,
            generated.GeneratorVersion,
            proposal.MatchedNameCount,
            userId,
            checkedIds,
            proposal.SupportingExampleIds);

        if (creationResult.IsFailure)
        {
            return creationResult.Error;
        }

        return await _draftRepository.SaveAsync(creationResult.Value, cancellationToken).ConfigureAwait(false);
    }
}