using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.ImportBatches.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.ImportBatches.GetMyCatalogImportBatches;

public sealed class GetMyCatalogImportBatchesQueryHandler
{
    private const int MaximumPageSize = 200;

    private readonly ICatalogImportBatchRepository _importBatchRepository;
    private readonly IUserRepository _userRepository;

    public GetMyCatalogImportBatchesQueryHandler(
        ICatalogImportBatchRepository importBatchRepository,
        IUserRepository userRepository)
    {
        _importBatchRepository = importBatchRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<GetMyCatalogImportBatchesResult, DomainError>> Handle(
        GetMyCatalogImportBatchesQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.CurrentUserId == Guid.Empty)
        {
            return Result.Failure<GetMyCatalogImportBatchesResult, DomainError>(
                CatalogImportErrors.CurrentUserNotFound());
        }

        if (query.Page < 1
            || query.PageSize < 1
            || query.PageSize > MaximumPageSize)
        {
            return Result.Failure<GetMyCatalogImportBatchesResult, DomainError>(
                CatalogImportErrors.InvalidBatchListPagination());
        }

        if (query.Status == CatalogImportBatchStatus.None)
        {
            return Result.Failure<GetMyCatalogImportBatchesResult, DomainError>(
                CatalogImportErrors.InvalidBatchStatusFilter(
                    CatalogImportBatchStatus.None));
        }

        var skipLong = ((long)query.Page - 1) * query.PageSize;

        if (skipLong > int.MaxValue)
        {
            return Result.Failure<GetMyCatalogImportBatchesResult, DomainError>(
                CatalogImportErrors.InvalidBatchListPagination());
        }

        var currentUser = await _userRepository
            .GetByIdAsync(query.CurrentUserId, cancellationToken)
            .ConfigureAwait(false);

        if (currentUser is null)
        {
            return Result.Failure<GetMyCatalogImportBatchesResult, DomainError>(
                CatalogImportErrors.CurrentUserNotFound());
        }

        if (!currentUser.CanViewOwnCatalogImports())
        {
            return Result.Failure<GetMyCatalogImportBatchesResult, DomainError>(
                CatalogImportErrors.UserCannotViewOwnCatalogImports());
        }

        var batches = await _importBatchRepository
            .GetByCreatorAsync(
                currentUser.Id,
                query.Status,
                (int)skipLong,
                query.PageSize,
                cancellationToken)
            .ConfigureAwait(false);

        var totalCount = await _importBatchRepository
            .CountByCreatorAsync(
                currentUser.Id,
                query.Status,
                cancellationToken)
            .ConfigureAwait(false);

        var items = batches
            .Select(batch => CreateItem(batch, currentUser))
            .ToArray();

        var totalPages = totalCount == 0
            ? 0
            : ((totalCount - 1) / query.PageSize) + 1;

        var result = new GetMyCatalogImportBatchesResult(
            items,
            query.Page,
            query.PageSize,
            totalCount,
            totalPages);

        return Result.Success<GetMyCatalogImportBatchesResult, DomainError>(
            result);
    }

    private static MyCatalogImportBatchItemResult CreateItem(
        CatalogImportBatch batch,
        ElectronicService.Domain.Users.User currentUser)
    {
        var canEdit =
            currentUser.CanEditCatalogImport()
            && batch.IsEditable;

        var canSubmit =
            currentUser.CanSubmitCatalogImportForReview()
            && batch.Status == CatalogImportBatchStatus.Ready;

        var canApply =
            currentUser.CanApplyCatalogImport()
            && batch.Status == CatalogImportBatchStatus.Ready;

        var canDelete =
            currentUser.CanDeleteOwnCatalogImport()
            && batch.CanBeDeletedByOwner;

        return new MyCatalogImportBatchItemResult(
            batch.Id,
            batch.ProductTypeId,
            batch.OriginalFileName,
            batch.FileSizeBytes,
            batch.Status,
            batch.RowsCount,
            batch.ValidRowsCount,
            batch.ErrorRowsCount,
            batch.CreatedAtUtc,
            batch.UpdatedAtUtc,
            batch.UpdatedAtUtc ?? batch.CreatedAtUtc,
            batch.SubmittedAtUtc,
            batch.ChangesRequestedAtUtc,
            batch.ChangesRequestComment,
            batch.RejectedAtUtc,
            batch.RejectionReason,
            batch.AppliedAtUtc,
            batch.Version,
            canEdit,
            canSubmit,
            canApply,
            canDelete);
    }
}