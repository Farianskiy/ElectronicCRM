using System.Text.Json;
using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.ImportBatches.Abstractions;
using ElectronicService.Core.Catalog.ImportBatches.Analysis;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Core.Users.Access;
using ElectronicService.Domain.Common;
using ElectronicService.Domain.Users.Enums;

namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed class CatalogRecognitionRuleSetBatchPreviewService
{
    private const int PageSize = 25;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IUserRepository _userRepository;
    private readonly IUserPermissionOverrideRepository _permissionRepository;
    private readonly ICatalogImportBatchRepository _batchRepository;
    private readonly ICatalogRecognitionRuleSetVersionReader _versionReader;
    private readonly CatalogRecognitionRuleSetNamePreviewService _namePreviewService;

    public CatalogRecognitionRuleSetBatchPreviewService(
        ICurrentUserProvider currentUserProvider,
        IUserRepository userRepository,
        IUserPermissionOverrideRepository permissionRepository,
        ICatalogImportBatchRepository batchRepository,
        ICatalogRecognitionRuleSetVersionReader versionReader,
        CatalogRecognitionRuleSetNamePreviewService namePreviewService)
    {
        _currentUserProvider = currentUserProvider;
        _userRepository = userRepository;
        _permissionRepository = permissionRepository;
        _batchRepository = batchRepository;
        _versionReader = versionReader;
        _namePreviewService = namePreviewService;
    }

    public async Task<Result<CatalogRecognitionRuleSetBatchPreviewPage, DomainError>>
        PreviewAsync(
            Guid versionId,
            Guid batchId,
            int page,
            CancellationToken cancellationToken = default)
    {
        if (_currentUserProvider.UserId is not Guid userId ||
            userId == Guid.Empty)
        {
            return new DomainError(
                "training.unauthorized",
                "Необходимо войти в систему.");
        }

        if (versionId == Guid.Empty ||
            batchId == Guid.Empty ||
            page < 1 ||
            page > 10000)
        {
            return new DomainError(
                "training.invalid_request",
                "Укажите версию, пакет и страницу от 1 до 10000.");
        }

        var user = await _userRepository.GetByIdAsync(
            userId,
            cancellationToken).ConfigureAwait(false);

        if (user is null || !user.IsActive)
        {
            return new DomainError(
                "training.forbidden",
                "Учётная запись недоступна.");
        }

        var overrides = await _permissionRepository.GetByUserIdAsync(
            userId,
            cancellationToken).ConfigureAwait(false);

        var permissionOverride = overrides.SingleOrDefault(item =>
            item.PermissionCode == UserPermissionCode.DictionariesManage);

        var hasPermission = permissionOverride?.IsAllowed ??
            UserPermissionCatalog.GetDefaults(user.Type)
                .Contains(UserPermissionCode.DictionariesManage);

        if (!hasPermission)
        {
            return new DomainError(
                "training.forbidden",
                "Требуется право управления справочниками.");
        }

        var batch = await _batchRepository.GetByIdAsync(
            batchId,
            cancellationToken).ConfigureAwait(false);

        if (batch is null)
        {
            return new DomainError(
                "training.not_found",
                "Пакет импорта не найден.");
        }

        if (batch.CreatedByUserId != userId)
        {
            return new DomainError(
                "training.forbidden",
                "Проверка доступна только для своего пакета импорта.");
        }

        var version = await _versionReader.GetByIdAsync(
            versionId,
            cancellationToken).ConfigureAwait(false);

        if (version is null)
        {
            return new DomainError(
                "training.not_found",
                "Версия правил не найдена.");
        }

        var rows = await _batchRepository.GetRowsAsync(
            batchId,
            null,
            null,
            null,
            null,
            null,
            (page - 1) * PageSize,
            PageSize + 1,
            cancellationToken).ConfigureAwait(false);

        var items = new List<CatalogRecognitionRuleSetBatchPreviewItem>(
            PageSize);

        foreach (var row in rows.Take(PageSize))
        {
            cancellationToken.ThrowIfCancellationRequested();

            CatalogImportNormalizedRowData? data;

            try
            {
                data = JsonSerializer.Deserialize<CatalogImportNormalizedRowData>(
                    row.NormalizedDataJson,
                    JsonOptions);
            }
            catch (JsonException)
            {
                return new DomainError(
                    "training.invalid_data",
                    $"Не удалось прочитать данные строки {row.RowNumber}.");
            }

            if (data is null)
            {
                return new DomainError(
                    "training.invalid_data",
                    $"Отсутствуют данные строки {row.RowNumber}.");
            }

            var productTypeId = data.ProductTypeId ?? batch.ProductTypeId;

            if (data.ManufacturerId != version.ManufacturerId ||
                productTypeId != version.ProductTypeId)
            {
                items.Add(new CatalogRecognitionRuleSetBatchPreviewItem(
                    row.Id,
                    row.RowNumber,
                    data.Name,
                    "OutsideScope",
                    null));

                continue;
            }

            if (string.IsNullOrWhiteSpace(data.Name) ||
                data.Name.Length > 2000)
            {
                return new DomainError(
                    "training.invalid_data",
                    $"В строке {row.RowNumber} отсутствует название или превышена длина 2000 символов.");
            }

            var preview = await _namePreviewService.PreviewAsync(
                versionId,
                version.ManufacturerId,
                version.ProductTypeId,
                data.Name,
                cancellationToken).ConfigureAwait(false);

            if (preview.IsFailure)
            {
                return new DomainError(
                    preview.Error.Code,
                    $"Строка {row.RowNumber}: {preview.Error.Message}");
            }

            if (data.Characteristics is null)
            {
                return new DomainError(
                    "training.invalid_data",
                    $"Отсутствуют данные характеристик строки {row.RowNumber}.");
            }

            var comparisons =
                CatalogRecognitionRuleSetCurrentValueComparer.Compare(
                    preview.Value,
                    data.Characteristics);

            items.Add(new CatalogRecognitionRuleSetBatchPreviewItem(
                row.Id,
                row.RowNumber,
                data.Name,
                GetStatus(preview.Value.Resolution),
                preview.Value)
            {
                Comparisons = comparisons
            });
        }

        return new CatalogRecognitionRuleSetBatchPreviewPage(
            batchId,
            versionId,
            version.VersionNumber,
            DateTime.UtcNow,
            page,
            PageSize,
            rows.Count > PageSize,
            items);
    }

    private static string GetStatus(
        CatalogRecognitionRuleSetValueResolution resolution)
    {
        if (resolution.HasConflicts)
        {
            return "Conflict";
        }

        return resolution.Characteristics.Count == 0
            ? "NoMatch"
            : "Proposed";
    }
}