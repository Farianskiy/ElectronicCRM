using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.ImportBatches.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.ImportBatches.GetCatalogImportReviewQueue;

public sealed class GetCatalogImportReviewQueueQueryHandler
{
    private const int MaximumPageSize = 200;

    private readonly ICatalogImportBatchRepository _importBatchRepository;
    private readonly IUserRepository _userRepository;

    public GetCatalogImportReviewQueueQueryHandler(
        ICatalogImportBatchRepository importBatchRepository,
        IUserRepository userRepository)
    {
        _importBatchRepository = importBatchRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<GetCatalogImportReviewQueueResult, DomainError>> Handle(
        GetCatalogImportReviewQueueQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.CurrentUserId == Guid.Empty)
        {
            return Result.Failure<GetCatalogImportReviewQueueResult, DomainError>(
                CatalogImportErrors.CurrentUserNotFound());
        }

        if (query.Page < 1
            || query.PageSize < 1
            || query.PageSize > MaximumPageSize)
        {
            return Result.Failure<GetCatalogImportReviewQueueResult, DomainError>(
                CatalogImportErrors.InvalidPagination());
        }

        if (query.Status.HasValue
            && query.Status.Value != CatalogImportBatchStatus.Submitted
            && query.Status.Value != CatalogImportBatchStatus.UnderReview)
        {
            return Result.Failure<GetCatalogImportReviewQueueResult, DomainError>(
                CatalogImportErrors.InvalidReviewQueueStatus(query.Status.Value));
        }

        var skipLong = ((long)query.Page - 1) * query.PageSize;

        if (skipLong > int.MaxValue)
        {
            return Result.Failure<GetCatalogImportReviewQueueResult, DomainError>(
                CatalogImportErrors.InvalidPagination());
        }

        var currentUser = await _userRepository
            .GetByIdAsync(query.CurrentUserId, cancellationToken)
            .ConfigureAwait(false);

        if (currentUser is null)
        {
            return Result.Failure<GetCatalogImportReviewQueueResult, DomainError>(
                CatalogImportErrors.CurrentUserNotFound());
        }

        if (!currentUser.CanReviewCatalogImports())
        {
            return Result.Failure<GetCatalogImportReviewQueueResult, DomainError>(
                CatalogImportErrors.UserCannotReviewCatalogImports());
        }

        var batches = await _importBatchRepository
            .GetReviewQueueAsync(
                query.Status,
                (int)skipLong,
                query.PageSize,
                cancellationToken)
            .ConfigureAwait(false);

        var totalCount = await _importBatchRepository
            .CountReviewQueueAsync(
                query.Status,
                cancellationToken)
            .ConfigureAwait(false);

        var creatorIds = batches
            .Select(batch => batch.CreatedByUserId)
            .Distinct()
            .ToArray();

        var creators = await _userRepository
            .GetByIdsAsync(creatorIds, cancellationToken)
            .ConfigureAwait(false);

        var creatorsById = creators.ToDictionary(user => user.Id);

        var items = batches
            .Select(batch =>
            {
                creatorsById.TryGetValue(
                    batch.CreatedByUserId,
                    out var creator);

                return new CatalogImportReviewQueueItemResult(
                    batch.Id,
                    batch.CreatedByUserId,
                    creator?.DisplayName.Value ?? "Неизвестный пользователь",
                    creator?.Email?.Value,
                    creator?.Type.ToString() ?? "Unknown",
                    batch.ProductTypeId,
                    batch.OriginalFileName,
                    batch.Status,
                    batch.RowsCount,
                    batch.ValidRowsCount,
                    batch.ErrorRowsCount,
                    batch.CreatedAtUtc,
                    batch.SubmittedAtUtc,
                    batch.ReviewedByUserId,
                    batch.ReviewedAtUtc,
                    batch.Version);
            })
            .ToArray();

        var totalPages = totalCount == 0
            ? 0
            : ((totalCount - 1) / query.PageSize) + 1;

        var result = new GetCatalogImportReviewQueueResult(
            items,
            query.Page,
            query.PageSize,
            totalCount,
            totalPages);

        return Result.Success<GetCatalogImportReviewQueueResult, DomainError>(result);
    }
}