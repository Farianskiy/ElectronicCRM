using System.Text.Json;
using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.CharacteristicDefinitions.Abstractions;
using ElectronicService.Core.Catalog.ImportBatches.Abstractions;
using ElectronicService.Core.Catalog.ImportBatches.Analysis;
using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Core.Users.Access;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Common;
using ElectronicService.Domain.Users.Enums;

namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed class CatalogRecognitionIntegerBatchPreviewService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IUserRepository _userRepository;
    private readonly IUserPermissionOverrideRepository _permissionRepository;
    private readonly ICatalogImportBatchRepository _batchRepository;
    private readonly ICharacteristicDefinitionRepository _definitionRepository;
    private readonly ICatalogProductMetadataRepository _metadataRepository;
    private readonly CatalogRecognitionTrainingDataLoader _loader;

    public CatalogRecognitionIntegerBatchPreviewService(
        ICurrentUserProvider currentUserProvider,
        IUserRepository userRepository,
        IUserPermissionOverrideRepository permissionRepository,
        ICatalogImportBatchRepository batchRepository,
        ICharacteristicDefinitionRepository definitionRepository,
        ICatalogProductMetadataRepository metadataRepository,
        CatalogRecognitionTrainingDataLoader loader)
    {
        _currentUserProvider = currentUserProvider;
        _userRepository = userRepository;
        _permissionRepository = permissionRepository;
        _batchRepository = batchRepository;
        _definitionRepository = definitionRepository;
        _metadataRepository = metadataRepository;
        _loader = loader;
    }

    public async Task<Result<CatalogRecognitionIntegerBatchPreviewPage, DomainError>> PreviewAsync(
        CatalogRecognitionIntegerBatchPreviewRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_currentUserProvider.UserId is not Guid userId || userId == Guid.Empty)
        {
            return new DomainError("training.unauthorized", "Необходимо войти в систему.");
        }

        if (request.Scope is null || request.BatchId == Guid.Empty || request.Scope.ManufacturerId == Guid.Empty || request.Scope.ProductTypeId == Guid.Empty || request.Scope.CharacteristicDefinitionId == Guid.Empty)
        {
            return new DomainError("training.invalid_request", "Укажите пакет, производителя, тип товара и характеристику.");
        }

        if (request.Page < 1 || request.Page > 10000 || request.PageSize < 1 || request.PageSize > 100)
        {
            return new DomainError("training.invalid_request", "Номер страницы должен быть от 1 до 10000, размер страницы — от 1 до 100.");
        }

        if (!IsValidPattern(request.Pattern))
        {
            return new DomainError("training.invalid_request", "Некорректное описание структурного предложения.");
        }

        if (!string.Equals(request.GeneratorVersion, CatalogRecognitionIntegerAlternativesGenerator.Version, StringComparison.Ordinal))
        {
            return new DomainError("training.conflict", "Версия генератора изменилась. Получите предложения повторно.");
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

        var batch = await _batchRepository.GetByIdAsync(request.BatchId, cancellationToken).ConfigureAwait(false);

        if (batch is null)
        {
            return new DomainError("training.not_found", "Пакет импорта не найден.");
        }

        if (batch.CreatedByUserId != userId)
        {
            return new DomainError("training.forbidden", "Проверка доступна только для своего пакета импорта.");
        }

        var manufacturer = await _metadataRepository.GetManufacturerByIdAsync(request.Scope.ManufacturerId, cancellationToken).ConfigureAwait(false);
        var productType = await _metadataRepository.GetProductTypeByIdAsync(request.Scope.ProductTypeId, cancellationToken).ConfigureAwait(false);

        if (manufacturer is null || productType is null)
        {
            return new DomainError("training.not_found", "Производитель или тип товара не найден.");
        }

        if (!productType.Characteristics.Any(item => item.CharacteristicDefinitionId == request.Scope.CharacteristicDefinitionId))
        {
            return new DomainError("training.invalid_request", "Характеристика не относится к выбранному типу товара.");
        }

        var definition = await _definitionRepository.GetByIdAsync(request.Scope.CharacteristicDefinitionId, cancellationToken).ConfigureAwait(false);

        if (definition is null || definition.DataType != CharacteristicDataType.Number)
        {
            return new DomainError("training.invalid_request", "Требуется существующая числовая характеристика.");
        }

        var prepared = await _loader.LoadAsync(request.Scope, cancellationToken).ConfigureAwait(false);

        if (!prepared.CanGenerate)
        {
            return new DomainError("training.conflict", "Учебная выборка недоступна, неполна или содержит проблемы. Обновите структурные предложения и проверьте диагностику.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var generated = CatalogRecognitionIntegerAlternativesGenerator.Generate(prepared);
        var proposal = generated.Proposals.SingleOrDefault(item => PatternsEqual(item.Pattern, request.Pattern));

        if (proposal is null || !proposal.PassedExamples)
        {
            return new DomainError("training.conflict", "Предложение изменилось или больше не проходит проверку. Получите структурные предложения повторно.");
        }

        var trainingNames = prepared.Samples.Select(item => item.ProductName).ToHashSet(StringComparer.Ordinal);
        var skip = (request.Page - 1) * request.PageSize;
        var rows = await _batchRepository.GetRowsAsync(request.BatchId, null, null, null, null, null, skip, request.PageSize + 1, cancellationToken).ConfigureAwait(false);
        var results = new List<CatalogRecognitionIntegerBatchPreviewItem>(request.PageSize);

        foreach (var row in rows.Take(request.PageSize))
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

            data.Characteristics.TryGetValue(request.Scope.CharacteristicDefinitionId.ToString(), out var currentValue);

            var input = new CatalogRecognitionLiteralRowPreviewInput(
                row.Id,
                row.RowNumber,
                data.Name,
                data.ManufacturerId,
                data.ProductTypeId ?? batch.ProductTypeId,
                currentValue);

            var result = CatalogRecognitionIntegerRowPreviewEvaluator.Evaluate(request.Scope, proposal.Pattern, input);
            var matchesTrainingName = data.Name is not null && trainingNames.Contains(data.Name);

            results.Add(new CatalogRecognitionIntegerBatchPreviewItem(result, matchesTrainingName));
        }

        return new CatalogRecognitionIntegerBatchPreviewPage(
            request.BatchId,
            request.Scope,
            generated.GeneratorVersion,
            proposal.Pattern,
            DateTime.UtcNow,
            request.Page,
            request.PageSize,
            rows.Count > request.PageSize,
            results,
            generated.Issues);
    }

    private static bool IsValidPattern(CatalogRecognitionIntegerAlternativesPattern? pattern)
    {
        if (pattern is null || pattern.Prefix is null || pattern.Prefix.Length > 2000 || pattern.Suffixes is null || pattern.Suffixes.Count == 0 || pattern.Suffixes.Count > 16)
        {
            return false;
        }

        if (pattern.Suffixes.Any(suffix => suffix is null || suffix.Length > 2000 || (pattern.Prefix.Length == 0 && suffix.Length == 0)))
        {
            return false;
        }

        return pattern.Suffixes.Distinct(StringComparer.Ordinal).Count() == pattern.Suffixes.Count;
    }

    private static bool PatternsEqual(
        CatalogRecognitionIntegerAlternativesPattern first,
        CatalogRecognitionIntegerAlternativesPattern second)
    {
        return string.Equals(first.Prefix, second.Prefix, StringComparison.Ordinal) && first.Suffixes.Count == second.Suffixes.Count && first.Suffixes.ToHashSet(StringComparer.Ordinal).SetEquals(second.Suffixes);
    }
}