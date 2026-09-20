using System.Text.Json;
using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Common;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Repositories;

public sealed class CatalogRecognitionRuleSetReportReader
    : ICatalogRecognitionRuleSetReportReader
{
    private const int PageSize = 25;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly ElectronicDbContext _dbContext;
    private readonly ICurrentUserProvider _currentUserProvider;

    public CatalogRecognitionRuleSetReportReader(
        ElectronicDbContext dbContext,
        ICurrentUserProvider currentUserProvider)
    {
        _dbContext = dbContext;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<Result<CatalogRecognitionRuleSetReportPage, DomainError>>
        GetPageAsync(
            Guid reportId,
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

        if (reportId == Guid.Empty || page < 1 || page > 10000)
        {
            return new DomainError(
                "training.invalid_request",
                "Укажите отчёт и страницу от 1 до 10000.");
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
            return new DomainError(
                "training.not_found",
                "Отчёт не найден или недоступен.");
        }

        if (report.SnapshotFormatVersion != 1)
        {
            return new DomainError(
                "training.invalid_report",
                "Формат сохранённого отчёта не поддерживается.");
        }

        CatalogRecognitionRuleSetBatchPreviewItem[]? rows;

        try
        {
            rows = JsonSerializer.Deserialize<
                CatalogRecognitionRuleSetBatchPreviewItem[]>(
                    report.SnapshotJson,
                    JsonOptions);
        }
        catch (JsonException)
        {
            return new DomainError(
                "training.invalid_report",
                "Не удалось прочитать сохранённый снимок отчёта.");
        }

        if (rows is null ||
            rows.Length != report.TotalRowsCount ||
            rows.Any(item => item is null))
        {
            return new DomainError(
                "training.invalid_report",
                "Сохранённый снимок не соответствует количеству строк отчёта.");
        }

        if (rows.Select(item => item.RowId).Distinct().Count() != rows.Length)
        {
            return new DomainError(
                "training.invalid_report",
                "Сохранённый снимок содержит повторяющиеся строки.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var skip = (page - 1) * PageSize;
        var items = rows.Skip(skip).Take(PageSize).ToArray();

        var summary = new CatalogRecognitionRuleSetReportCreated(
            report.Id,
            report.RuleSetVersionId,
            report.BatchId,
            report.CompletedAtUtc,
            report.TotalRowsCount,
            report.ProposedRowsCount,
            report.ConflictRowsCount,
            report.NoMatchRowsCount,
            report.OutsideScopeRowsCount);

        return new CatalogRecognitionRuleSetReportPage(
            summary,
            report.StartedAtUtc,
            report.BatchVersion,
            report.EvaluatorVersion,
            report.SnapshotFormatVersion,
            page,
            PageSize,
            skip + items.Length < rows.Length,
            items);
    }

    public async Task<Result<
    IReadOnlyList<CatalogRecognitionRuleSetReportCreated>,
    DomainError>> GetRecentAsync(
        Guid versionId,
        Guid batchId,
        CancellationToken cancellationToken = default)
    {
        if (_currentUserProvider.UserId is not Guid userId ||
            userId == Guid.Empty)
        {
            return new DomainError(
                "training.unauthorized",
                "Необходимо войти в систему.");
        }

        if (versionId == Guid.Empty || batchId == Guid.Empty)
        {
            return new DomainError(
                "training.invalid_request",
                "Укажите версию правил и пакет импорта.");
        }

        var items = await _dbContext.CatalogRecognitionRuleSetReports
            .AsNoTracking()
            .Where(report =>
                report.CreatedByUserId == userId &&
                report.RuleSetVersionId == versionId &&
                report.BatchId == batchId)
            .OrderByDescending(report => report.CompletedAtUtc)
            .ThenByDescending(report => report.Id)
            .Take(20)
            .Select(report => new CatalogRecognitionRuleSetReportCreated(
                report.Id,
                report.RuleSetVersionId,
                report.BatchId,
                report.CompletedAtUtc,
                report.TotalRowsCount,
                report.ProposedRowsCount,
                report.ConflictRowsCount,
                report.NoMatchRowsCount,
                report.OutsideScopeRowsCount))
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result.Success<
            IReadOnlyList<CatalogRecognitionRuleSetReportCreated>,
            DomainError>(items);
    }
}