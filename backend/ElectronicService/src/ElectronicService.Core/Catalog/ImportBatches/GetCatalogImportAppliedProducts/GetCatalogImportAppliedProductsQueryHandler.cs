using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.ImportBatches.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.ImportBatches.GetCatalogImportAppliedProducts;

public sealed class GetCatalogImportAppliedProductsQueryHandler
{
    private const int MaximumPageSize = 200;

    private readonly ICatalogImportBatchRepository _importBatchRepository;
    private readonly ICatalogImportAppliedProductsReader _reader;
    private readonly IUserRepository _userRepository;

    public GetCatalogImportAppliedProductsQueryHandler(
        ICatalogImportBatchRepository importBatchRepository,
        ICatalogImportAppliedProductsReader reader,
        IUserRepository userRepository)
    {
        _importBatchRepository = importBatchRepository;
        _reader = reader;
        _userRepository = userRepository;
    }

    public async Task<Result<GetCatalogImportAppliedProductsResult, DomainError>> Handle(
        GetCatalogImportAppliedProductsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.CurrentUserId == Guid.Empty)
        {
            return Result.Failure<GetCatalogImportAppliedProductsResult, DomainError>(
                CatalogImportErrors.CurrentUserNotFound());
        }

        if (query.BatchId == Guid.Empty)
        {
            return Result.Failure<GetCatalogImportAppliedProductsResult, DomainError>(
                CatalogImportErrors.BatchNotFound(query.BatchId));
        }

        if (query.Page < 1 || query.PageSize < 1 || query.PageSize > MaximumPageSize)
        {
            return Result.Failure<GetCatalogImportAppliedProductsResult, DomainError>(
                CatalogImportErrors.InvalidPagination());
        }

        var skipLong = ((long)query.Page - 1) * query.PageSize;

        if (skipLong > int.MaxValue)
        {
            return Result.Failure<GetCatalogImportAppliedProductsResult, DomainError>(
                CatalogImportErrors.InvalidPagination());
        }

        var currentUser = await _userRepository
            .GetByIdAsync(query.CurrentUserId, cancellationToken)
            .ConfigureAwait(false);

        if (currentUser is null)
        {
            return Result.Failure<GetCatalogImportAppliedProductsResult, DomainError>(
                CatalogImportErrors.CurrentUserNotFound());
        }

        var batch = await _importBatchRepository
            .GetByIdAsync(query.BatchId, cancellationToken)
            .ConfigureAwait(false);

        if (batch is null)
        {
            return Result.Failure<GetCatalogImportAppliedProductsResult, DomainError>(
                CatalogImportErrors.BatchNotFound(query.BatchId));
        }

        var isOwner = batch.CreatedByUserId == currentUser.Id;
        var canReview = currentUser.CanReviewCatalogImports();

        if (!isOwner && !canReview)
        {
            return Result.Failure<GetCatalogImportAppliedProductsResult, DomainError>(
                CatalogImportErrors.UserCannotAccessBatch());
        }

        if (batch.Status != CatalogImportBatchStatus.Applied)
        {
            return Result.Failure<GetCatalogImportAppliedProductsResult, DomainError>(
                CatalogImportErrors.AppliedProductsUnavailable(batch.Status));
        }

        var readResult = await _reader
            .ReadAsync(
                batch.Id,
                (int)skipLong,
                query.PageSize,
                cancellationToken)
            .ConfigureAwait(false);

        var totalPages = readResult.TotalCount == 0
            ? 0
            : ((readResult.TotalCount - 1) / query.PageSize) + 1;

        var result = new GetCatalogImportAppliedProductsResult(
            batch.Id,
            readResult.Items,
            query.Page,
            query.PageSize,
            readResult.TotalCount,
            totalPages);

        return Result.Success<GetCatalogImportAppliedProductsResult, DomainError>(
            result);
    }
}