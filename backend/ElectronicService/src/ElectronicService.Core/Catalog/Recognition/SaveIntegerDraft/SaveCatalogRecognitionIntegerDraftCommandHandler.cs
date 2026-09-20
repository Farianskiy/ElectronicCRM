using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.CharacteristicDefinitions.Abstractions;
using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Core.Users;
using ElectronicService.Core.Users.Access;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Domain.Common;
using ElectronicService.Domain.Users.Enums;

namespace ElectronicService.Core.Catalog.Recognition.SaveIntegerDraft;

public sealed class SaveCatalogRecognitionIntegerDraftCommandHandler
{
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IUserRepository _userRepository;
    private readonly IUserPermissionOverrideRepository _permissionRepository;
    private readonly ICatalogProductMetadataRepository _metadataRepository;
    private readonly ICharacteristicDefinitionRepository _definitionRepository;
    private readonly CatalogRecognitionTrainingDataLoader _loader;
    private readonly ICatalogRecognitionIntegerDraftRepository _draftRepository;

    public SaveCatalogRecognitionIntegerDraftCommandHandler(
        ICurrentUserProvider currentUserProvider,
        IUserRepository userRepository,
        IUserPermissionOverrideRepository permissionRepository,
        ICatalogProductMetadataRepository metadataRepository,
        ICharacteristicDefinitionRepository definitionRepository,
        CatalogRecognitionTrainingDataLoader loader,
        ICatalogRecognitionIntegerDraftRepository draftRepository)
    {
        _currentUserProvider = currentUserProvider;
        _userRepository = userRepository;
        _permissionRepository = permissionRepository;
        _metadataRepository = metadataRepository;
        _definitionRepository = definitionRepository;
        _loader = loader;
        _draftRepository = draftRepository;
    }

    public async Task<Result<Guid, DomainError>> Handle(SaveCatalogRecognitionIntegerDraftCommand command, CancellationToken cancellationToken = default)
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

        if (command.ManufacturerId == Guid.Empty || command.ProductTypeId == Guid.Empty || command.CharacteristicDefinitionId == Guid.Empty)
        {
            return new DomainError("training.invalid_request", "Укажите производителя, тип товара и характеристику.");
        }

        if (!IsValidPattern(command))
        {
            return new DomainError("training.invalid_request", "Некорректное начало или окончания числового шаблона.");
        }

        if (!string.Equals(command.GeneratorVersion, CatalogRecognitionIntegerAlternativesGenerator.Version, StringComparison.Ordinal))
        {
            return new DomainError("training.conflict", "Версия генератора изменилась. Получите структурные предложения повторно.");
        }

        var manufacturer = await _metadataRepository.GetManufacturerByIdAsync(command.ManufacturerId, cancellationToken).ConfigureAwait(false);
        var productType = await _metadataRepository.GetProductTypeByIdAsync(command.ProductTypeId, cancellationToken).ConfigureAwait(false);

        if (manufacturer is null || productType is null)
        {
            return new DomainError("training.not_found", "Производитель или тип товара не найден.");
        }

        if (!productType.Characteristics.Any(item => item.CharacteristicDefinitionId == command.CharacteristicDefinitionId))
        {
            return new DomainError("training.invalid_request", "Характеристика не относится к выбранному типу товара.");
        }

        var definition = await _definitionRepository.GetByIdAsync(command.CharacteristicDefinitionId, cancellationToken).ConfigureAwait(false);

        if (definition is null || definition.DataType != CharacteristicDataType.Number)
        {
            return new DomainError("training.invalid_request", "Для числового шаблона требуется существующая числовая характеристика.");
        }

        var scope = new CatalogRecognitionTrainingScope(command.ManufacturerId, command.ProductTypeId, command.CharacteristicDefinitionId);
        var prepared = await _loader.LoadAsync(scope, cancellationToken).ConfigureAwait(false);

        if (!prepared.CanGenerate)
        {
            return new DomainError("training.conflict", "Учебные примеры не прошли подготовку. Обновите предложения и проверьте диагностику.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var generated = CatalogRecognitionIntegerAlternativesGenerator.Generate(prepared);
        var requestedSuffixes = command.Suffixes.ToHashSet(StringComparer.Ordinal);

        var proposal = generated.Proposals.SingleOrDefault(candidate =>
            string.Equals(candidate.Pattern.Prefix, command.Prefix, StringComparison.Ordinal) &&
            candidate.Pattern.Suffixes.Count == requestedSuffixes.Count &&
            requestedSuffixes.SetEquals(candidate.Pattern.Suffixes));

        if (proposal is null || !proposal.PassedExamples)
        {
            return new DomainError("training.conflict", "Выбранное предложение изменилось или больше не проходит проверку. Получите структурные предложения повторно.");
        }

        var checkedIds = prepared.Samples.SelectMany(sample => sample.ExampleIds).Distinct().ToArray();

        var creationResult = CatalogRecognitionIntegerDraft.Create(
            command.ManufacturerId,
            command.ProductTypeId,
            command.CharacteristicDefinitionId,
            proposal.Pattern.Prefix,
            proposal.Pattern.Suffixes.ToArray(),
            generated.GeneratorVersion,
            proposal.MatchedNameCount,
            proposal.DistinctValueCount,
            userId,
            checkedIds,
            proposal.SupportingExampleIds.ToArray());

        if (creationResult.IsFailure)
        {
            return creationResult.Error;
        }

        cancellationToken.ThrowIfCancellationRequested();

        return await _draftRepository.SaveAsync(creationResult.Value, cancellationToken).ConfigureAwait(false);
    }

    private static bool IsValidPattern(SaveCatalogRecognitionIntegerDraftCommand command)
    {
        if (command.Prefix is null || command.Prefix.Length > 2000 || command.Suffixes is null || command.Suffixes.Count == 0 || command.Suffixes.Count > 16)
        {
            return false;
        }

        if (command.Suffixes.Any(suffix => suffix is null || suffix.Length > 2000 || (command.Prefix.Length == 0 && suffix.Length == 0)))
        {
            return false;
        }

        return command.Suffixes.Distinct(StringComparer.Ordinal).Count() == command.Suffixes.Count;
    }
}