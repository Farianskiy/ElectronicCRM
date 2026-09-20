using System.Text.Json;
using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.ImportBatches.Abstractions;
using ElectronicService.Core.Catalog.ImportBatches.Analysis;
using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Core.Users.Access;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Domain.Common;
using ElectronicService.Domain.Users.Enums;

namespace ElectronicService.Core.Catalog.Recognition.ConfirmTrainingExample;

public sealed class ConfirmCatalogRecognitionTrainingExampleCommandHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IUserRepository _userRepository;
    private readonly IUserPermissionOverrideRepository _permissionRepository;
    private readonly ICatalogImportBatchRepository _batchRepository;
    private readonly ICatalogProductMetadataRepository _metadataRepository;
    private readonly ICatalogRecognitionFeedbackRepository _feedbackRepository;
    private readonly ICatalogRecognitionTrainingExampleRepository _exampleRepository;

    public ConfirmCatalogRecognitionTrainingExampleCommandHandler(
        ICurrentUserProvider currentUserProvider,
        IUserRepository userRepository,
        IUserPermissionOverrideRepository permissionRepository,
        ICatalogImportBatchRepository batchRepository,
        ICatalogProductMetadataRepository metadataRepository,
        ICatalogRecognitionFeedbackRepository feedbackRepository,
        ICatalogRecognitionTrainingExampleRepository exampleRepository)
    {
        _currentUserProvider = currentUserProvider;
        _userRepository = userRepository;
        _permissionRepository = permissionRepository;
        _batchRepository = batchRepository;
        _metadataRepository = metadataRepository;
        _feedbackRepository = feedbackRepository;
        _exampleRepository = exampleRepository;
    }

    public async Task<Result<Guid, DomainError>> Handle(ConfirmCatalogRecognitionTrainingExampleCommand command, CancellationToken cancellationToken = default)
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
            return new DomainError("training.forbidden", "Для подтверждения примеров требуется право управления справочниками.");
        }

        if (command.BatchId == Guid.Empty || command.RowId == Guid.Empty || command.CharacteristicDefinitionId == Guid.Empty)
        {
            return new DomainError("training.invalid_request", "Не указан пакет, строка или характеристика.");
        }

        var batch = await _batchRepository.GetByIdAsync(command.BatchId, cancellationToken).ConfigureAwait(false);

        if (batch is null)
        {
            return new DomainError("training.not_found", "Пакет импорта не найден.");
        }

        if (batch.CreatedByUserId != userId)
        {
            return new DomainError("training.forbidden", "Подтверждение доступно только для своего пакета импорта.");
        }

        var row = await _batchRepository.GetRowByIdAsync(command.BatchId, command.RowId, cancellationToken).ConfigureAwait(false);

        if (row is null)
        {
            return new DomainError("training.not_found", "Строка импорта не найдена.");
        }

        var data = DeserializeData(row.NormalizedDataJson);

        if (data is null || data.Characteristics is null)
        {
            return new DomainError("training.invalid_data", "Не удалось прочитать сохранённые данные строки.");
        }

        var feedback = await _feedbackRepository.GetByImportRowAndCharacteristicAsync(command.RowId, command.CharacteristicDefinitionId, cancellationToken).ConfigureAwait(false);

        if (feedback is null || feedback.ImportBatchId != command.BatchId)
        {
            return new DomainError("training.not_found", "Сохранённая разметка характеристики не найдена. Сначала сохраните строку.");
        }

        if (!MatchesCommand(feedback, command))
        {
            return new DomainError("training.conflict", "Разметка изменилась. Обновите строку и проверьте выделение повторно.");
        }

        var effectiveProductTypeId = data.ProductTypeId ?? batch.ProductTypeId;

        if (!string.Equals(data.Name?.Trim(), feedback.ProductName, StringComparison.Ordinal) || data.ManufacturerId != feedback.ManufacturerId || effectiveProductTypeId != feedback.ProductTypeId)
        {
            return new DomainError("training.conflict", "Название, производитель или тип товара больше не соответствуют разметке.");
        }

        var matchingValues = data.Characteristics.Where(pair => Guid.TryParse(pair.Key, out var definitionId) && definitionId == command.CharacteristicDefinitionId).ToArray();

        if (matchingValues.Length != 1 || !string.Equals(matchingValues[0].Value?.Trim(), feedback.FinalNormalizedValue, StringComparison.Ordinal))
        {
            return new DomainError("training.conflict", "Значение характеристики изменилось или отсутствует. Сначала сохраните правильное значение и разметку.");
        }

        var productType = await _metadataRepository.GetProductTypeByIdAsync(feedback.ProductTypeId, cancellationToken).ConfigureAwait(false);

        if (productType is null || !productType.Characteristics.Any(value => value.CharacteristicDefinitionId == command.CharacteristicDefinitionId))
        {
            return new DomainError("training.invalid_data", "Характеристика не относится к текущему типу товара.");
        }

        var manufacturer = await _metadataRepository.GetManufacturerByIdAsync(command.ManufacturerId, cancellationToken).ConfigureAwait(false);

        if (manufacturer is null)
        {
            return new DomainError("training.invalid_data", "Производитель не найден.");
        }

        var creationResult = CatalogRecognitionTrainingExample.Create(feedback, userId);

        if (creationResult.IsFailure)
        {
            return creationResult.Error;
        }

        var candidate = creationResult.Value;
        var saved = await _exampleRepository.AddOrGetActiveAsync(candidate, cancellationToken).ConfigureAwait(false);

        if (!MatchesExample(saved, candidate))
        {
            return new DomainError("training.conflict", "Для этой характеристики уже подтверждён другой пример. Сначала необходимо отозвать прежнее подтверждение.");
        }

        return saved.Id;
    }

    private static bool MatchesCommand(CatalogRecognitionFeedback feedback, ConfirmCatalogRecognitionTrainingExampleCommand command)
    {
        return feedback.ManufacturerId == command.ManufacturerId
            && feedback.ProductTypeId == command.ProductTypeId
            && feedback.ConfirmedSpanStart == command.SpanStart
            && feedback.ConfirmedSpanLength == command.SpanLength
            && string.Equals(feedback.ProductName, command.ProductName, StringComparison.Ordinal)
            && string.Equals(feedback.FinalNormalizedValue, command.NormalizedValue, StringComparison.Ordinal)
            && string.Equals(feedback.ConfirmedRawValue, command.RawValue, StringComparison.Ordinal);
    }

    private static bool MatchesExample(CatalogRecognitionTrainingExample existing, CatalogRecognitionTrainingExample candidate)
    {
        return existing.SourceFeedbackId == candidate.SourceFeedbackId
            && existing.ManufacturerId == candidate.ManufacturerId
            && existing.ProductTypeId == candidate.ProductTypeId
            && existing.CharacteristicDefinitionId == candidate.CharacteristicDefinitionId
            && existing.SpanStart == candidate.SpanStart
            && existing.SpanLength == candidate.SpanLength
            && string.Equals(existing.ProductName, candidate.ProductName, StringComparison.Ordinal)
            && string.Equals(existing.RawValue, candidate.RawValue, StringComparison.Ordinal)
            && string.Equals(existing.NormalizedValue, candidate.NormalizedValue, StringComparison.Ordinal);
    }

    private static CatalogImportNormalizedRowData? DeserializeData(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<CatalogImportNormalizedRowData>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }
}