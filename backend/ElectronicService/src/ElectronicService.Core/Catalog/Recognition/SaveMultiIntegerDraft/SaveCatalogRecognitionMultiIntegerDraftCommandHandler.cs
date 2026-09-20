using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Core.Users;
using ElectronicService.Core.Users.Access;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Domain.Common;
using ElectronicService.Domain.Users.Enums;

namespace ElectronicService.Core.Catalog.Recognition.SaveMultiIntegerDraft;

public sealed class SaveCatalogRecognitionMultiIntegerDraftCommandHandler
{
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IUserRepository _userRepository;
    private readonly IUserPermissionOverrideRepository _permissionRepository;
    private readonly CatalogRecognitionMultiIntegerProposalService _proposalService;
    private readonly ICatalogRecognitionMultiIntegerDraftRepository _draftRepository;

    public SaveCatalogRecognitionMultiIntegerDraftCommandHandler(
        ICurrentUserProvider currentUserProvider,
        IUserRepository userRepository,
        IUserPermissionOverrideRepository permissionRepository,
        CatalogRecognitionMultiIntegerProposalService proposalService,
        ICatalogRecognitionMultiIntegerDraftRepository draftRepository)
    {
        _currentUserProvider = currentUserProvider;
        _userRepository = userRepository;
        _permissionRepository = permissionRepository;
        _proposalService = proposalService;
        _draftRepository = draftRepository;
    }

    public async Task<Result<Guid, DomainError>> Handle(
        SaveCatalogRecognitionMultiIntegerDraftCommand command,
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

        if (!CatalogRecognitionMultiIntegerPatternValidator.IsValid(command.Pattern))
        {
            return new DomainError("training.invalid_request", "Некорректная структура числового шаблона.");
        }

        if (!string.Equals(command.GeneratorVersion, CatalogRecognitionMultiIntegerProposalGenerator.Version, StringComparison.Ordinal))
        {
            return new DomainError("training.conflict", "Версия генератора изменилась. Получите предложения повторно.");
        }

        var previewResult = await _proposalService.PreviewAsync(
            command.ManufacturerId,
            command.ProductTypeId,
            command.CharacteristicDefinitionIds,
            command.ProductNames,
            cancellationToken).ConfigureAwait(false);

        if (previewResult.IsFailure)
        {
            return previewResult.Error;
        }

        var generated = previewResult.Value;

        if (generated.Issues.Count > 0 || generated.CheckedExampleIds.Count == 0)
        {
            return new DomainError("training.conflict", "Учебные примеры не прошли проверку. Обновите предложения и проверьте диагностику.");
        }

        var proposal = generated.Proposals.SingleOrDefault(candidate =>
            candidate.Pattern.Parts.SequenceEqual(command.Pattern.Parts));

        if (proposal is null || !proposal.PassedExamples)
        {
            return new DomainError("training.conflict", "Выбранное предложение больше не проходит проверку или изменилось. Получите предложения повторно.");
        }

        var coverageByCharacteristic = proposal.Fields.ToDictionary(
            coverage => coverage.CharacteristicDefinitionId,
            coverage => coverage.DistinctValueCount);

        var parts = proposal.Pattern.Parts.Select(part =>
        {
            var distinctValueCount = 0;

            if (part.CharacteristicDefinitionId is Guid characteristicId)
            {
                distinctValueCount = coverageByCharacteristic[characteristicId];
            }

            return new CatalogRecognitionMultiIntegerDraftPartData(
                part.Literal,
                part.CharacteristicDefinitionId,
                distinctValueCount);
        }).ToArray();

        var creationResult = CatalogRecognitionMultiIntegerDraft.Create(
            new CatalogRecognitionMultiIntegerDraftData(
                command.ManufacturerId,
                command.ProductTypeId,
                generated.GeneratorVersion,
                parts,
                proposal.MatchedNameCount,
                proposal.SupportingNameCount,
                userId,
                generated.CheckedExampleIds.ToArray(),
                proposal.SupportingExampleIds.ToArray()));

        if (creationResult.IsFailure)
        {
            return creationResult.Error;
        }

        cancellationToken.ThrowIfCancellationRequested();

        return await _draftRepository.SaveAsync(
            creationResult.Value,
            command.ProductNames,
            cancellationToken).ConfigureAwait(false);
    }
}