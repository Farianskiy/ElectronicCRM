using System.Text.Json;
using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.CharacteristicDefinitions.Abstractions;
using ElectronicService.Core.Catalog.ImportBatches.Abstractions;
using ElectronicService.Core.Catalog.ImportBatches.Analysis;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Core.Users.Access;
using ElectronicService.Domain.Common;
using ElectronicService.Domain.Users.Enums;

namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed class CatalogRecognitionLiteralBatchPreviewService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IUserRepository _userRepository;
    private readonly IUserPermissionOverrideRepository _permissionRepository;
    private readonly ICatalogImportBatchRepository _batchRepository;
    private readonly ICatalogRecognitionLiteralDraftReader _draftReader;
    private readonly ICharacteristicDefinitionRepository _definitionRepository;

    public CatalogRecognitionLiteralBatchPreviewService(
        ICurrentUserProvider currentUserProvider,
        IUserRepository userRepository,
        IUserPermissionOverrideRepository permissionRepository,
        ICatalogImportBatchRepository batchRepository,
        ICatalogRecognitionLiteralDraftReader draftReader,
        ICharacteristicDefinitionRepository definitionRepository)
    {
        _currentUserProvider = currentUserProvider;
        _userRepository = userRepository;
        _permissionRepository = permissionRepository;
        _batchRepository = batchRepository;
        _draftReader = draftReader;
        _definitionRepository = definitionRepository;
    }

    public async Task<Result<CatalogRecognitionLiteralBatchPreviewPage, DomainError>> PreviewAsync(
        Guid draftId,
        Guid batchId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (_currentUserProvider.UserId is not Guid userId || userId == Guid.Empty)
        {
            return new DomainError("training.unauthorized", "Необходимо войти в систему.");
        }

        if (draftId == Guid.Empty || batchId == Guid.Empty)
        {
            return new DomainError("training.invalid_request", "Укажите черновик и пакет импорта.");
        }

        if (page < 1 || page > 10000 || pageSize < 1 || pageSize > 100)
        {
            return new DomainError("training.invalid_request", "Номер страницы должен быть от 1 до 10000, размер страницы — от 1 до 100.");
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
            return new DomainError("training.forbidden", "Для проверки требуется право управления справочниками.");
        }

        var batch = await _batchRepository.GetByIdAsync(batchId, cancellationToken).ConfigureAwait(false);

        if (batch is null)
        {
            return new DomainError("training.not_found", "Пакет импорта не найден.");
        }

        if (batch.CreatedByUserId != userId)
        {
            return new DomainError("training.forbidden", "Проверка доступна только для своего пакета импорта.");
        }

        var details = await _draftReader.GetByIdAsync(draftId, cancellationToken).ConfigureAwait(false);

        if (details is null)
        {
            return new DomainError("training.not_found", "Черновик не найден.");
        }

        var definition = await _definitionRepository.GetByIdAsync(details.Draft.CharacteristicDefinitionId, cancellationToken).ConfigureAwait(false);

        if (definition is null)
        {
            return new DomainError("training.not_found", "Характеристика черновика не найдена.");
        }

        var skip = (page - 1) * pageSize;
        var rows = await _batchRepository.GetRowsAsync(batchId, null, null, null, null, null, skip, pageSize + 1, cancellationToken).ConfigureAwait(false);
        var results = new List<CatalogRecognitionLiteralRowPreviewResult>(pageSize);

        foreach (var row in rows.Take(pageSize))
        {
            cancellationToken.ThrowIfCancellationRequested();

            CatalogImportNormalizedRowData? data;

            try
            {
                data = JsonSerializer.Deserialize<CatalogImportNormalizedRowData>(row.NormalizedDataJson, JsonOptions);
            }
            catch (JsonException)
            {
                return new DomainError("training.invalid_data", $"Не удалось прочитать данные строки {row.RowNumber}.");
            }

            if (data is null || data.Characteristics is null)
            {
                return new DomainError("training.invalid_data", $"Отсутствуют нормализованные данные строки {row.RowNumber}.");
            }

            data.Characteristics.TryGetValue(details.Draft.CharacteristicDefinitionId.ToString(), out var currentValue);

            var input = new CatalogRecognitionLiteralRowPreviewInput(
                row.Id,
                row.RowNumber,
                data.Name,
                data.ManufacturerId,
                data.ProductTypeId ?? batch.ProductTypeId,
                currentValue);

            results.Add(CatalogRecognitionLiteralRowPreviewEvaluator.Evaluate(details.Draft, input));
        }

        return new CatalogRecognitionLiteralBatchPreviewPage(
            draftId,
            batchId,
            DateTime.UtcNow,
            page,
            pageSize,
            rows.Count > pageSize,
            results);
    }
}