using System.Text.Json;
using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.ImportBatches.Abstractions;
using ElectronicService.Core.Catalog.ImportBatches.Analysis;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Domain.Common;
using ElectronicService.Domain.Catalog.Manufacturers;

namespace ElectronicService.Core.Catalog.ImportBatches.GetCatalogImportRows;

public sealed class
    GetCatalogImportRowsQueryHandler
{
    private const int MaximumPageSize = 200;

    private const int MaximumSearchLength = 100;

    private const int MaximumIssueCodeLength = 100;

    private const int MaximumManufacturerGroupKeyLength = 200;

    private static readonly JsonSerializerOptions
        JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

    private readonly ICatalogImportBatchRepository
        _importBatchRepository;

    private readonly IUserRepository
        _userRepository;

    public GetCatalogImportRowsQueryHandler(
        ICatalogImportBatchRepository
            importBatchRepository,
        IUserRepository userRepository)
    {
        _importBatchRepository =
            importBatchRepository;

        _userRepository =
            userRepository;
    }

    public async Task<Result<
        GetCatalogImportRowsResult,
        DomainError>> Handle(
            GetCatalogImportRowsQuery query,
            CancellationToken cancellationToken =
                default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.CurrentUserId == Guid.Empty)
        {
            return Result.Failure<
                GetCatalogImportRowsResult,
                DomainError>(
                    CatalogImportErrors
                        .CurrentUserNotFound());
        }

        if (query.BatchId == Guid.Empty)
        {
            return Result.Failure<
                GetCatalogImportRowsResult,
                DomainError>(
                    CatalogImportErrors
                        .BatchNotFound(
                            query.BatchId));
        }

        if (query.Page < 1
            || query.PageSize < 1
            || query.PageSize > MaximumPageSize)
        {
            return Result.Failure<
                GetCatalogImportRowsResult,
                DomainError>(
                    CatalogImportErrors
                        .InvalidPagination());
        }

        var search =
        string.IsNullOrWhiteSpace(query.Search)
            ? null
            : query.Search.Trim();

        if (search?.Length > MaximumSearchLength)
        {
            return Result.Failure<
                GetCatalogImportRowsResult,
                DomainError>(
                    CatalogImportErrors
                        .RowsSearchIsTooLong(
                            MaximumSearchLength));
        }

        var issueCode =
            string.IsNullOrWhiteSpace(query.IssueCode)
                ? null
                : query.IssueCode.Trim();

        if (issueCode is not null
            && (issueCode.Length > MaximumIssueCodeLength
                || issueCode.Any(character =>
                    !IsIssueCodeCharacter(character))))
        {
            return Result.Failure<
                GetCatalogImportRowsResult,
                DomainError>(
                    CatalogImportErrors
                        .InvalidRowIssueCode());
        }

        var manufacturerGroupKey =
            string.IsNullOrWhiteSpace(query.ManufacturerGroupKey)
                ? null
                : ManufacturerNameNormalizer.Normalize(query.ManufacturerGroupKey);

        if (manufacturerGroupKey?.Length > MaximumManufacturerGroupKeyLength)
        {
            return Result.Failure<GetCatalogImportRowsResult, DomainError>(
                GeneralErrors.ValueIsInvalid(nameof(query.ManufacturerGroupKey)));
        }

        var skipLong =
            ((long)query.Page - 1)
            * query.PageSize;

        if (skipLong > int.MaxValue)
        {
            return Result.Failure<
                GetCatalogImportRowsResult,
                DomainError>(
                    CatalogImportErrors
                        .InvalidPagination());
        }

        var currentUser =
            await _userRepository
                .GetByIdAsync(
                    query.CurrentUserId,
                    cancellationToken)
                .ConfigureAwait(false);

        if (currentUser is null)
        {
            return Result.Failure<
                GetCatalogImportRowsResult,
                DomainError>(
                    CatalogImportErrors
                        .CurrentUserNotFound());
        }

        var batch =
            await _importBatchRepository
                .GetByIdAsync(
                    query.BatchId,
                    cancellationToken)
                .ConfigureAwait(false);

        if (batch is null)
        {
            return Result.Failure<
                GetCatalogImportRowsResult,
                DomainError>(
                    CatalogImportErrors
                        .BatchNotFound(
                            query.BatchId));
        }

        var isOwner =
            batch.CreatedByUserId
            == currentUser.Id;

        var canRead =
            isOwner
            || currentUser
                .CanReviewCatalogImports();

        if (!canRead)
        {
            return Result.Failure<
                GetCatalogImportRowsResult,
                DomainError>(
                    CatalogImportErrors
                        .UserCannotAccessBatch());
        }

        var skip = (int)skipLong;

        var rows = await _importBatchRepository
            .GetRowsAsync(
                query.BatchId,
                query.Status,
                search,
                issueCode,
                query.ProblemKind,
                manufacturerGroupKey,
                skip,
                query.PageSize,
                cancellationToken)
            .ConfigureAwait(false);

        var totalCount = await _importBatchRepository
            .CountRowsAsync(
                query.BatchId,
                query.Status,
                search,
                issueCode,
                query.ProblemKind,
                manufacturerGroupKey,
                cancellationToken)
            .ConfigureAwait(false);

        var items =
            new List<GetCatalogImportRowResult>(
                rows.Count);

        try
        {
            foreach (var row in rows)
            {
                var rawData =
                    JsonSerializer.Deserialize<
                        IReadOnlyDictionary<
                            int,
                            string>>(
                                row.RawDataJson,
                                JsonOptions);

                var normalizedData =
                    JsonSerializer.Deserialize<
                        CatalogImportNormalizedRowData>(
                            row.NormalizedDataJson,
                            JsonOptions);

                var issues =
                    JsonSerializer.Deserialize<
                        IReadOnlyCollection<
                            CatalogImportRowIssue>>(
                                row.IssuesJson,
                                JsonOptions);

                var warnings =
                    JsonSerializer.Deserialize<
                        IReadOnlyCollection<
                            CatalogImportRowIssue>>(
                                row.WarningsJson,
                                JsonOptions);

                if (rawData is null
                    || normalizedData is null
                    || issues is null
                    || warnings is null)
                {
                    return Result.Failure<
                        GetCatalogImportRowsResult,
                        DomainError>(
                            CatalogImportErrors
                                .InvalidImportJson(
                                    nameof(row)));
                }

                items.Add(
                    new GetCatalogImportRowResult(
                        row.Id,
                        row.RowNumber,
                        row.Status,
                        rawData,
                        normalizedData,
                        issues,
                        warnings));
            }
        }
        catch (JsonException)
        {
            return Result.Failure<
                GetCatalogImportRowsResult,
                DomainError>(
                    CatalogImportErrors
                        .InvalidImportJson(
                            "catalogImportRow"));
        }

        var totalPages =
            totalCount == 0
                ? 0
                : ((totalCount - 1)
                    / query.PageSize)
                  + 1;

        var result =
            new GetCatalogImportRowsResult(
                items,
                query.Page,
                query.PageSize,
                totalCount,
                totalPages);

        return Result.Success<
            GetCatalogImportRowsResult,
            DomainError>(result);
    }

    private static bool IsIssueCodeCharacter(
    char character)
    {
        return character is >= 'a' and <= 'z'
            or >= '0' and <= '9'
            or '.'
            or '-'
            or '_';
    }
}