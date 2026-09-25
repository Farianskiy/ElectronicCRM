using System.Data;
using System.Text.Json;
using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.ImportBatches.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Domain.Common;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Repositories;

public sealed class CatalogRecognitionRuleSetReportCreator
    : ICatalogRecognitionRuleSetReportCreator
{
    private const int MaxRowsCount = 2000;
    private const string EvaluatorVersion = "rule-set-report-v1";

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly IServiceScopeFactory _scopeFactory;

    public CatalogRecognitionRuleSetReportCreator(
        IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<Result<CatalogRecognitionRuleSetReportCreated, DomainError>> CreateAsync(Guid versionId, Guid batchId, CancellationToken cancellationToken = default)
    {
        await using var settingsScope = _scopeFactory.CreateAsyncScope();
        var options = settingsScope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ElectronicService.Core.Catalog.Recognition.Evaluation.RecognitionEvaluationOptions>>().Value;
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));
        try { return await CreateCoreAsync(versionId, batchId, deadline.Token).ConfigureAwait(false); }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        { return new DomainError("evaluation.timeout", "Проверка превысила допустимое время; частичный отчёт не сохранён."); }
    }

    private async Task<Result<CatalogRecognitionRuleSetReportCreated, DomainError>> CreateCoreAsync(Guid versionId, Guid batchId, CancellationToken cancellationToken)
    {
        if (versionId == Guid.Empty || batchId == Guid.Empty)
        {
            return new DomainError(
                "training.invalid_request",
                "Укажите версию правил и пакет импорта.");
        }

        // Новый scope исключает ранее загруженные отслеживаемые сущности.
        await using var scope = _scopeFactory.CreateAsyncScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<ElectronicDbContext>();

        var previewService = scope.ServiceProvider
            .GetRequiredService<CatalogRecognitionRuleSetBatchPreviewService>();

        var batchRepository = scope.ServiceProvider
            .GetRequiredService<ICatalogImportBatchRepository>();

        var currentUser = scope.ServiceProvider
            .GetRequiredService<ICurrentUserProvider>();

        if (currentUser.UserId is not Guid userId || userId == Guid.Empty)
        {
            return new DomainError(
                "training.unauthorized",
                "Необходимо войти в систему.");
        }

        var startedAtUtc = DateTime.UtcNow;

        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(
                IsolationLevel.RepeatableRead,
                cancellationToken)
            .ConfigureAwait(false);

        // Существующий сервис проверяет активность пользователя,
        // право DictionariesManage и принадлежность пакета.
        var firstPage = await previewService.PreviewAsync(
            versionId,
            batchId,
            1,
            cancellationToken).ConfigureAwait(false);

        if (firstPage.IsFailure)
        {
            return firstPage.Error;
        }

        var totalRows = await batchRepository.CountRowsAsync(
            batchId,
            null,
            null,
            null,
            null,
            null,
            cancellationToken).ConfigureAwait(false);

        if (totalRows < 1 || totalRows > MaxRowsCount)
        {
            return new DomainError(
                "training.selection_too_large",
                $"Для этой операции пакет должен содержать от 1 до {MaxRowsCount} строк.");
        }

        var batchVersion = await batchRepository.GetVersionAsync(
            batchId,
            cancellationToken).ConfigureAwait(false);

        if (!batchVersion.HasValue)
        {
            return new DomainError(
                "training.not_found",
                "Пакет импорта не найден.");
        }

        var items = new List<CatalogRecognitionRuleSetBatchPreviewItem>(
            totalRows);

        var currentPage = firstPage.Value;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            items.AddRange(currentPage.Items);

            if (items.Count > totalRows)
            {
                return new DomainError(
                    "training.invalid_report",
                    "Получено больше строк, чем содержит пакет.");
            }

            if (!currentPage.HasMore)
            {
                break;
            }

            if (currentPage.Items.Count == 0 || items.Count >= totalRows)
            {
                return new DomainError(
                    "training.invalid_report",
                    "Нарушена последовательность страниц проверки.");
            }

            var nextPage = await previewService.PreviewAsync(
                versionId,
                batchId,
                currentPage.Page + 1,
                cancellationToken).ConfigureAwait(false);

            if (nextPage.IsFailure)
            {
                return nextPage.Error;
            }

            currentPage = nextPage.Value;
        }

        if (items.Count != totalRows ||
            items.Select(item => item.RowId).Distinct().Count() != totalRows)
        {
            return new DomainError(
                "training.invalid_report",
                "Проверены не все строки или обнаружены повторяющиеся строки.");
        }

        var proposedRows = items.Count(item =>
            string.Equals(item.Status, "Proposed", StringComparison.Ordinal));

        var conflictRows = items.Count(item =>
            string.Equals(item.Status, "Conflict", StringComparison.Ordinal));

        var noMatchRows = items.Count(item =>
            string.Equals(item.Status, "NoMatch", StringComparison.Ordinal));

        var outsideScopeRows = items.Count(item =>
            string.Equals(item.Status, "OutsideScope", StringComparison.Ordinal));

        var snapshotJson = JsonSerializer.Serialize(items, JsonOptions);

        var creation = CatalogRecognitionRuleSetReport.Create(
            new CatalogRecognitionRuleSetReportData(
                versionId,
                batchId,
                batchVersion.Value,
                userId,
                startedAtUtc,
                DateTime.UtcNow,
                EvaluatorVersion,
                totalRows,
                proposedRows,
                conflictRows,
                noMatchRows,
                outsideScopeRows,
                snapshotJson));

        if (creation.IsFailure)
        {
            return creation.Error;
        }

        var comparison = await scope.ServiceProvider.GetRequiredService<RecognitionEvaluationService>()
            .CreateAsync(versionId, userId, cancellationToken).ConfigureAwait(false);
        if (comparison.IsFailure) return comparison.Error;
        var report = creation.Value;
        var attached = report.AttachEvaluation(ElectronicService.Core.Catalog.Recognition.Evaluation.EvaluationJson.Serialize(comparison.Value));
        if (attached.IsFailure) return attached.Error;

        dbContext.CatalogRecognitionRuleSetReports.Add(report);

        await dbContext.SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        await transaction.CommitAsync(cancellationToken)
            .ConfigureAwait(false);

        return new CatalogRecognitionRuleSetReportCreated(
            report.Id,
            report.RuleSetVersionId,
            report.BatchId,
            report.CompletedAtUtc,
            report.TotalRowsCount,
            report.ProposedRowsCount,
            report.ConflictRowsCount,
            report.NoMatchRowsCount,
            report.OutsideScopeRowsCount);
    }
}