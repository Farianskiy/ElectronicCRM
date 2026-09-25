using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.ImportBatches.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Core.Users;
using ElectronicService.Core.Users.Access;
using ElectronicService.Domain.Common;
using ElectronicService.Domain.Users.Enums;

namespace ElectronicService.Core.Catalog.Recognition.RevokeTrainingExample;

public sealed record RevokeCatalogRecognitionTrainingExampleCommand(Guid BatchId, Guid RowId, Guid ExampleId, string? Reason = null);

public sealed class RevokeCatalogRecognitionTrainingExampleCommandHandler
{
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IUserRepository _userRepository;
    private readonly IUserPermissionOverrideRepository _permissionRepository;
    private readonly ICatalogImportBatchRepository _batchRepository;
    private readonly ICatalogRecognitionFeedbackRepository _feedbackRepository;
    private readonly ICatalogRecognitionTrainingExampleRepository _exampleRepository;
    private readonly ICatalogTrainingExampleManagement _management;

    public RevokeCatalogRecognitionTrainingExampleCommandHandler(
        ICurrentUserProvider currentUserProvider,
        IUserRepository userRepository,
        IUserPermissionOverrideRepository permissionRepository,
        ICatalogImportBatchRepository batchRepository,
        ICatalogRecognitionFeedbackRepository feedbackRepository,
        ICatalogRecognitionTrainingExampleRepository exampleRepository,
        ICatalogTrainingExampleManagement management)
    {
        _currentUserProvider = currentUserProvider;
        _userRepository = userRepository;
        _permissionRepository = permissionRepository;
        _batchRepository = batchRepository;
        _feedbackRepository = feedbackRepository;
        _exampleRepository = exampleRepository;
        _management = management;
    }

    public async Task<UnitResult<DomainError>> Handle(RevokeCatalogRecognitionTrainingExampleCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (_currentUserProvider.UserId is not Guid userId || userId == Guid.Empty)
        {
            return UnitResult.Failure(new DomainError("training.unauthorized", "Необходимо войти в систему."));
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);

        if (user is null || !user.IsActive)
        {
            return UnitResult.Failure(new DomainError("training.forbidden", "Учётная запись недоступна."));
        }

        var overrides = await _permissionRepository.GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);
        var permissionOverride = overrides.SingleOrDefault(value => value.PermissionCode == UserPermissionCode.DictionariesManage);
        var hasPermission = permissionOverride?.IsAllowed ?? UserPermissionCatalog.GetDefaults(user.Type).Contains(UserPermissionCode.DictionariesManage);

        if (!hasPermission)
        {
            return UnitResult.Failure(new DomainError("training.forbidden", "Для отзыва требуется право управления справочниками."));
        }

        var batch = await _batchRepository.GetByIdAsync(command.BatchId, cancellationToken).ConfigureAwait(false);

        if (batch is null)
        {
            return UnitResult.Failure(new DomainError("training.not_found", "Пакет импорта не найден."));
        }

        if (batch.CreatedByUserId != userId)
        {
            return UnitResult.Failure(new DomainError("training.forbidden", "Отзыв доступен только для своего пакета импорта."));
        }

        var row = await _batchRepository.GetRowByIdAsync(command.BatchId, command.RowId, cancellationToken).ConfigureAwait(false);

        if (row is null)
        {
            return UnitResult.Failure(new DomainError("training.not_found", "Строка импорта не найдена."));
        }

        var example = await _exampleRepository.GetByIdAsync(command.ExampleId, cancellationToken).ConfigureAwait(false);

        if (example is null)
        {
            return UnitResult.Failure(new DomainError("training.not_found", "Учебный пример не найден."));
        }

        var feedback = await _feedbackRepository.GetByIdAsync(example.SourceFeedbackId, cancellationToken).ConfigureAwait(false);

        if (feedback is null || feedback.ImportBatchId != command.BatchId || feedback.ImportRowId != command.RowId)
        {
            return UnitResult.Failure(new DomainError("training.not_found", "Учебный пример не относится к указанной строке."));
        }

        return await _management.RevokeAsync(example.Id,
            command.Reason ?? "legacy-api: причина не передана старым клиентом", cancellationToken).ConfigureAwait(false);
    }
}
