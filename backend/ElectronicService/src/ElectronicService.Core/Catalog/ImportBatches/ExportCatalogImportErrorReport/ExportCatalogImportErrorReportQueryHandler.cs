using System.Text.Json;
using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.ImportBatches.Abstractions;
using ElectronicService.Core.Catalog.ImportBatches.Analysis;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.ImportBatches.ExportCatalogImportErrorReport;

public sealed class ExportCatalogImportErrorReportQueryHandler
{
    private const string ExcelContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

    private readonly ICatalogImportBatchRepository _importBatchRepository;
    private readonly ICatalogImportErrorReportGenerator _reportGenerator;
    private readonly IUserRepository _userRepository;

    public ExportCatalogImportErrorReportQueryHandler(
        ICatalogImportBatchRepository importBatchRepository,
        ICatalogImportErrorReportGenerator reportGenerator,
        IUserRepository userRepository)
    {
        _importBatchRepository = importBatchRepository;
        _reportGenerator = reportGenerator;
        _userRepository = userRepository;
    }

    public async Task<Result<
        ExportCatalogImportErrorReportResult,
        DomainError>> Handle(
            ExportCatalogImportErrorReportQuery query,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.CurrentUserId == Guid.Empty)
        {
            return Result.Failure<
                ExportCatalogImportErrorReportResult,
                DomainError>(
                    CatalogImportErrors.CurrentUserNotFound());
        }

        if (query.BatchId == Guid.Empty)
        {
            return Result.Failure<
                ExportCatalogImportErrorReportResult,
                DomainError>(
                    CatalogImportErrors.BatchNotFound(
                        query.BatchId));
        }

        var currentUser = await _userRepository
            .GetByIdAsync(
                query.CurrentUserId,
                cancellationToken)
            .ConfigureAwait(false);

        if (currentUser is null)
        {
            return Result.Failure<
                ExportCatalogImportErrorReportResult,
                DomainError>(
                    CatalogImportErrors.CurrentUserNotFound());
        }

        var batch = await _importBatchRepository
            .GetByIdAsync(
                query.BatchId,
                cancellationToken)
            .ConfigureAwait(false);

        if (batch is null)
        {
            return Result.Failure<
                ExportCatalogImportErrorReportResult,
                DomainError>(
                    CatalogImportErrors.BatchNotFound(
                        query.BatchId));
        }

        var isOwner =
            batch.CreatedByUserId == currentUser.Id;

        var canReview =
            currentUser.CanReviewCatalogImports();

        if (!isOwner && !canReview)
        {
            return Result.Failure<
                ExportCatalogImportErrorReportResult,
                DomainError>(
                    CatalogImportErrors.UserCannotAccessBatch());
        }

        var errorRowsCount = await _importBatchRepository
            .CountRowsAsync(
                batch.Id,
                CatalogImportRowStatus.Error,
                cancellationToken)
            .ConfigureAwait(false);

        if (errorRowsCount == 0)
        {
            return Result.Failure<
                ExportCatalogImportErrorReportResult,
                DomainError>(
                    CatalogImportErrors.ErrorReportUnavailable());
        }

        var columns = await _importBatchRepository
            .GetColumnsForAnalysisAsync(
                batch.Id,
                cancellationToken)
            .ConfigureAwait(false);

        var rows = await _importBatchRepository
            .GetRowsAsync(
                batch.Id,
                CatalogImportRowStatus.Error,
                skip: 0,
                take: errorRowsCount,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var reportColumns = columns
            .OrderBy(column => column.SourceColumnNumber)
            .Select(column =>
                new CatalogImportErrorReportColumn(
                    column.SourceColumnNumber,
                    column.SourceHeader))
            .ToArray();

        var reportRows =
            new List<CatalogImportErrorReportRow>(
                rows.Count);

        try
        {
            foreach (
                var row in rows.OrderBy(
                    item => item.RowNumber))
            {
                var rawData = JsonSerializer.Deserialize<
                    IReadOnlyDictionary<int, string>>(
                        row.RawDataJson,
                        JsonOptions);

                var issues = JsonSerializer.Deserialize<
                    IReadOnlyCollection<CatalogImportRowIssue>>(
                        row.IssuesJson,
                        JsonOptions);

                var warnings = JsonSerializer.Deserialize<
                    IReadOnlyCollection<CatalogImportRowIssue>>(
                        row.WarningsJson,
                        JsonOptions);

                if (
                    rawData is null ||
                    issues is null ||
                    warnings is null
                )
                {
                    return Result.Failure<
                        ExportCatalogImportErrorReportResult,
                        DomainError>(
                            CatalogImportErrors.InvalidImportJson(
                                nameof(row)));
                }

                reportRows.Add(
                    new CatalogImportErrorReportRow(
                        row.RowNumber,
                        rawData,
                        issues,
                        warnings));
            }
        }
        catch (JsonException)
        {
            return Result.Failure<
                ExportCatalogImportErrorReportResult,
                DomainError>(
                    CatalogImportErrors.InvalidImportJson(
                        "catalogImportRow"));
        }

        var reportData =
            new CatalogImportErrorReportData(
                batch.Id,
                batch.OriginalFileName,
                batch.Status.ToString(),
                batch.RowsCount,
                batch.ValidRowsCount,
                errorRowsCount,
                batch.CreatedAtUtc,
                DateTime.UtcNow,
                reportColumns,
                reportRows);

        var content =
            _reportGenerator.Generate(reportData);

        var result =
            new ExportCatalogImportErrorReportResult(
                CreateFileName(
                    batch.OriginalFileName,
                    batch.Id),
                ExcelContentType,
                content);

        return Result.Success<
            ExportCatalogImportErrorReportResult,
            DomainError>(result);
    }

    private static string CreateFileName(
        string originalFileName,
        Guid batchId)
    {
        var baseName =
            Path.GetFileNameWithoutExtension(
                originalFileName);

        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = "catalog-import";
        }

        foreach (
            var invalidCharacter in
            Path.GetInvalidFileNameChars())
        {
            baseName = baseName.Replace(
                invalidCharacter,
                '_');
        }

        return
            $"{baseName}_ошибки_{batchId:N}.xlsx";
    }
}