using System.Data;
using System.Text.Json;
using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.ImportBatches.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Common;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Repositories;

public sealed class CatalogRecognitionRuleSetActivationValidator
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly ElectronicDbContext _dbContext;
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly ICatalogImportBatchRepository _batchRepository;
    private readonly CatalogRecognitionRuleSetBatchPreviewService _previewService;
    private readonly CatalogRecognitionRuleSetTrainingCheckService _trainingService;

    public CatalogRecognitionRuleSetActivationValidator(
        ElectronicDbContext dbContext,
        ICurrentUserProvider currentUserProvider,
        ICatalogImportBatchRepository batchRepository,
        CatalogRecognitionRuleSetBatchPreviewService previewService,
        CatalogRecognitionRuleSetTrainingCheckService trainingService)
    {
        _dbContext = dbContext;
        _currentUserProvider = currentUserProvider;
        _batchRepository = batchRepository;
        _previewService = previewService;
        _trainingService = trainingService;
    }

    public async Task<UnitResult<DomainError>> ValidateAsync(
        Guid versionId,
        Guid reportId,
        CancellationToken cancellationToken = default)
    {
        var transaction = _dbContext.Database.CurrentTransaction;

        if (transaction is null ||
            transaction.GetDbTransaction().IsolationLevel !=
                IsolationLevel.Serializable)
        {
            return Fail(
                "training.invalid_operation",
                "Проверка активации должна выполняться в Serializable-транзакции.");
        }

        if (_currentUserProvider.UserId is not Guid userId ||
            userId == Guid.Empty)
        {
            return Fail(
                "training.unauthorized",
                "Необходимо войти в систему.");
        }

        if (versionId == Guid.Empty || reportId == Guid.Empty)
        {
            return Fail(
                "training.invalid_request",
                "Укажите версию и отчёт проверки.");
        }

        var report = await _dbContext.CatalogRecognitionRuleSetReports
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == reportId &&
                    item.CreatedByUserId == userId,
                cancellationToken)
            .ConfigureAwait(false);

        if (report is null)
        {
            return Fail(
                "training.not_found",
                "Отчёт не найден или недоступен.");
        }

        if (report.RuleSetVersionId != versionId)
        {
            return Fail(
                "training.conflict",
                "Отчёт относится к другой версии правил.");
        }

        if (report.SnapshotFormatVersion != 1 ||
            !string.Equals(
                report.EvaluatorVersion,
                "rule-set-report-v1",
                StringComparison.Ordinal))
        {
            return Fail(
                "training.conflict",
                "Формат или исполнитель отчёта устарел. Создайте новый отчёт.");
        }

        if (report.TotalRowsCount < 1 || report.TotalRowsCount > 2000)
        {
            return Fail(
                "training.invalid_report",
                "Количество строк отчёта выходит за пределы текущего пилота.");
        }

        CatalogRecognitionRuleSetBatchPreviewItem[]? savedItems;

        try
        {
            savedItems = JsonSerializer.Deserialize<
                CatalogRecognitionRuleSetBatchPreviewItem[]>(
                    report.SnapshotJson,
                    JsonOptions);
        }
        catch (JsonException)
        {
            return Fail(
                "training.invalid_report",
                "Сохранённый снимок отчёта повреждён.");
        }

        if (savedItems is null ||
            savedItems.Length != report.TotalRowsCount ||
            savedItems.Any(item => item is null) ||
            savedItems.Select(item => item.RowId).Distinct().Count() !=
                savedItems.Length)
        {
            return Fail(
                "training.invalid_report",
                "Состав строк отчёта некорректен.");
        }

        // Здесь повторно проверяются права и принадлежность пакета.
        var firstPage = await _previewService.PreviewAsync(
            versionId,
            report.BatchId,
            1,
            cancellationToken).ConfigureAwait(false);

        if (firstPage.IsFailure)
        {
            return UnitResult.Failure(firstPage.Error);
        }

        var batchVersion = await _batchRepository.GetVersionAsync(
            report.BatchId,
            cancellationToken).ConfigureAwait(false);

        if (!batchVersion.HasValue ||
            (long)batchVersion.Value != report.BatchVersion)
        {
            return Fail(
                "training.conflict",
                "Пакет изменился после проверки. Создайте новый отчёт.");
        }

        var currentItems =
            new List<CatalogRecognitionRuleSetBatchPreviewItem>(
                savedItems.Length);

        var currentPage = firstPage.Value;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            currentItems.AddRange(currentPage.Items);

            if (currentItems.Count > savedItems.Length)
            {
                return Fail(
                    "training.conflict",
                    "Состав пакета изменился. Создайте новый отчёт.");
            }

            if (!currentPage.HasMore)
            {
                break;
            }

            if (currentPage.Items.Count == 0 ||
                currentItems.Count >= savedItems.Length)
            {
                return Fail(
                    "training.conflict",
                    "Состав пакета больше не соответствует отчёту.");
            }

            var nextPage = await _previewService.PreviewAsync(
                versionId,
                report.BatchId,
                currentPage.Page + 1,
                cancellationToken).ConfigureAwait(false);

            if (nextPage.IsFailure)
            {
                return UnitResult.Failure(nextPage.Error);
            }

            currentPage = nextPage.Value;
        }

        if (currentItems.Count != savedItems.Length ||
            !string.Equals(
                SerializeComparable(savedItems),
                SerializeComparable(currentItems),
                StringComparison.Ordinal))
        {
            return Fail(
                "training.conflict",
                "Результаты проверки изменились. Создайте и просмотрите новый отчёт.");
        }

        if (currentItems.Any(item => string.Equals(
                item.Status,
                "Conflict",
                StringComparison.Ordinal)))
        {
            return Fail(
                "training.conflict",
                "В отчёте есть конфликты между правилами.");
        }

        if (!currentItems.Any(item => string.Equals(
                item.Status,
                "Proposed",
                StringComparison.Ordinal)))
        {
            return Fail(
                "training.conflict",
                "Отчёт не содержит строк с предложенными значениями.");
        }

        var training = await _trainingService.CheckAsync(
            versionId,
            cancellationToken).ConfigureAwait(false);

        if (training.IsFailure)
        {
            return UnitResult.Failure(training.Error);
        }

        if (!training.Value.PassedTrainingChecks)
        {
            return Fail(
                "training.conflict",
                "Учебные основания версии изменились или не проходят проверку.");
        }

        return UnitResult.Success<DomainError>();
    }

    private static string SerializeComparable(
        IEnumerable<CatalogRecognitionRuleSetBatchPreviewItem> items)
    {
        // Время повторного запуска отличается по определению.
        // Остальные сохранённые результаты должны совпасть.
        var comparable = items
            .OrderBy(item => item.RowNumber)
            .ThenBy(item => item.RowId)
            .Select(item => item with
            {
                Preview = item.Preview is null
                    ? null
                    : item.Preview with { CheckedAtUtc = default }
            })
            .ToArray();

        return JsonSerializer.Serialize(comparable, JsonOptions);
    }

    private static UnitResult<DomainError> Fail(
        string code,
        string message)
    {
        return UnitResult.Failure(new DomainError(code, message));
    }
}