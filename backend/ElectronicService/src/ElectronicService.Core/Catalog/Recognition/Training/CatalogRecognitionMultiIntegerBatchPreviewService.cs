using System.Text.Json;
using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.ImportBatches.Abstractions;
using ElectronicService.Core.Catalog.ImportBatches.Analysis;
using ElectronicService.Core.Users;
using ElectronicService.Core.Users.Access;
using ElectronicService.Domain.Common;
using ElectronicService.Domain.Users.Enums;

namespace ElectronicService.Core.Catalog.Recognition.Training;

public sealed class CatalogRecognitionMultiIntegerBatchPreviewService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IUserRepository _userRepository;
    private readonly IUserPermissionOverrideRepository _permissionRepository;
    private readonly ICatalogImportBatchRepository _batchRepository;
    private readonly CatalogRecognitionMultiIntegerProposalService _proposalService;

    public CatalogRecognitionMultiIntegerBatchPreviewService(
        ICurrentUserProvider currentUserProvider,
        IUserRepository userRepository,
        IUserPermissionOverrideRepository permissionRepository,
        ICatalogImportBatchRepository batchRepository,
        CatalogRecognitionMultiIntegerProposalService proposalService)
    {
        _currentUserProvider = currentUserProvider;
        _userRepository = userRepository;
        _permissionRepository = permissionRepository;
        _batchRepository = batchRepository;
        _proposalService = proposalService;
    }

    public async Task<Result<CatalogRecognitionMultiIntegerBatchPreviewPage, DomainError>> PreviewAsync(
        CatalogRecognitionMultiIntegerBatchPreviewRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_currentUserProvider.UserId is not Guid userId || userId == Guid.Empty)
        {
            return new DomainError("training.unauthorized", "Необходимо войти в систему.");
        }

        if (request.BatchId == Guid.Empty || request.Page < 1 || request.Page > 10000 || request.PageSize < 1 || request.PageSize > 100)
        {
            return new DomainError("training.invalid_request", "Укажите пакет, страницу от 1 до 10000 и размер страницы от 1 до 100.");
        }

        if (!CatalogRecognitionMultiIntegerPatternValidator.IsValid(request.Pattern))
        {
            return new DomainError("training.invalid_request", "Передана некорректная структура шаблона.");
        }

        if (!string.Equals(request.GeneratorVersion, CatalogRecognitionMultiIntegerProposalGenerator.Version, StringComparison.Ordinal))
        {
            return new DomainError("training.conflict", "Версия генератора изменилась. Постройте составные предложения повторно.");
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

        var batch = await _batchRepository.GetByIdAsync(request.BatchId, cancellationToken).ConfigureAwait(false);

        if (batch is null)
        {
            return new DomainError("training.not_found", "Пакет импорта не найден.");
        }

        if (batch.CreatedByUserId != userId)
        {
            return new DomainError("training.forbidden", "Проверка доступна только для своего пакета импорта.");
        }

        var generation = await _proposalService.PreviewAsync(
            request.ManufacturerId,
            request.ProductTypeId,
            request.CharacteristicDefinitionIds,
            request.ProductNames,
            cancellationToken).ConfigureAwait(false);

        if (generation.IsFailure)
        {
            return generation.Error;
        }

        var generated = generation.Value;

        if (generated.Issues.Count > 0)
        {
            return new DomainError("training.conflict", "Учебная подборка больше не проходит подготовку. Постройте составные предложения повторно и просмотрите диагностику.");
        }

        var proposal = generated.Proposals.SingleOrDefault(item => item.Pattern.Parts.SequenceEqual(request.Pattern.Parts));

        if (proposal is null || !proposal.PassedExamples)
        {
            return new DomainError("training.conflict", "Выбранное предложение отсутствует или больше не проходит проверку. Постройте предложения повторно.");
        }

        var trainingNames = request.ProductNames.ToHashSet(StringComparer.Ordinal);
        var skip = (request.Page - 1) * request.PageSize;
        var rows = await _batchRepository.GetRowsAsync(request.BatchId, null, null, null, null, null, skip, request.PageSize + 1, cancellationToken).ConfigureAwait(false);
        var results = new List<CatalogRecognitionMultiIntegerBatchPreviewItem>(request.PageSize);

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

            var input = new CatalogRecognitionMultiIntegerRowPreviewInput(
                row.Id,
                row.RowNumber,
                data.Name,
                data.ManufacturerId,
                data.ProductTypeId ?? batch.ProductTypeId,
                data.Characteristics);

            var result = CatalogRecognitionMultiIntegerRowPreviewEvaluator.Evaluate(
                request.ManufacturerId,
                request.ProductTypeId,
                proposal.Pattern,
                input);

            var matchesTrainingName = data.Name is not null && trainingNames.Contains(data.Name);

            results.Add(new CatalogRecognitionMultiIntegerBatchPreviewItem(result, matchesTrainingName));
        }

        return new CatalogRecognitionMultiIntegerBatchPreviewPage(
            request.BatchId,
            generated.GeneratorVersion,
            proposal.Pattern,
            DateTime.UtcNow,
            request.Page,
            request.PageSize,
            rows.Count > request.PageSize,
            results);
    }
}