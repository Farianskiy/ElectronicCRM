using CSharpFunctionalExtensions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog
    .ImportBatches;
using ElectronicService.Domain.Common;
using ElectronicService.Domain.Users;

namespace ElectronicService.Core.Catalog
    .ImportBatches.Preview;

public sealed class CatalogImportPreviewQueryHandler
{
    private const int MaximumPageSize = 200;
    private const int MaximumPageNumber =
        1_000_000;

    private readonly ICatalogImportPreviewReader
        _previewReader;

    private readonly IUserRepository
        _userRepository;

    public CatalogImportPreviewQueryHandler(
        ICatalogImportPreviewReader
            previewReader,
        IUserRepository userRepository)
    {
        _previewReader = previewReader;
        _userRepository = userRepository;
    }

    public Task<Result<
        CatalogImportBatchDetailsResult,
        DomainError>> GetBatchAsync(
            Guid batchId,
            Guid currentUserId,
            CancellationToken cancellationToken =
                default)
    {
        return GetAuthorizedBatchAsync(
            batchId,
            currentUserId,
            cancellationToken);
    }

    public async Task<Result<
        IReadOnlyCollection<
            CatalogImportColumnResult>,
        DomainError>> GetColumnsAsync(
            Guid batchId,
            Guid currentUserId,
            CancellationToken cancellationToken =
                default)
    {
        var accessResult =
            await GetAuthorizedBatchAsync(
                    batchId,
                    currentUserId,
                    cancellationToken)
                .ConfigureAwait(false);

        if (accessResult.IsFailure)
        {
            return Result.Failure<
                IReadOnlyCollection<
                    CatalogImportColumnResult>,
                DomainError>(
                    accessResult.Error);
        }

        var columns =
            await _previewReader
                .GetColumnsAsync(
                    batchId,
                    cancellationToken)
                .ConfigureAwait(false);

        return Result.Success<
            IReadOnlyCollection<
                CatalogImportColumnResult>,
            DomainError>(
                columns);
    }

    public async Task<Result<
        CatalogImportRowsPageResult,
        DomainError>> GetRowsAsync(
            Guid batchId,
            Guid currentUserId,
            int pageNumber,
            int pageSize,
            CatalogImportRowStatus? status,
            CancellationToken cancellationToken =
                default)
    {
        if (pageNumber <= 0
            || pageNumber > MaximumPageNumber
            || pageSize <= 0
            || pageSize > MaximumPageSize)
        {
            return Result.Failure<
                CatalogImportRowsPageResult,
                DomainError>(
                    CatalogImportErrors
                        .InvalidPagination());
        }

        var accessResult =
            await GetAuthorizedBatchAsync(
                    batchId,
                    currentUserId,
                    cancellationToken)
                .ConfigureAwait(false);

        if (accessResult.IsFailure)
        {
            return Result.Failure<
                CatalogImportRowsPageResult,
                DomainError>(
                    accessResult.Error);
        }

        var rows =
            await _previewReader
                .GetRowsAsync(
                    batchId,
                    pageNumber,
                    pageSize,
                    status,
                    cancellationToken)
                .ConfigureAwait(false);

        return Result.Success<
            CatalogImportRowsPageResult,
            DomainError>(
                rows);
    }

    private async Task<Result<
        CatalogImportBatchDetailsResult,
        DomainError>> GetAuthorizedBatchAsync(
            Guid batchId,
            Guid currentUserId,
            CancellationToken cancellationToken)
    {
        if (batchId == Guid.Empty)
        {
            return Result.Failure<
                CatalogImportBatchDetailsResult,
                DomainError>(
                    CatalogImportErrors
                        .BatchNotFound(batchId));
        }

        if (currentUserId == Guid.Empty)
        {
            return Result.Failure<
                CatalogImportBatchDetailsResult,
                DomainError>(
                    CatalogImportErrors
                        .CurrentUserNotFound());
        }

        var user =
            await _userRepository
                .GetByIdAsync(
                    currentUserId,
                    cancellationToken)
                .ConfigureAwait(false);

        if (user is null)
        {
            return Result.Failure<
                CatalogImportBatchDetailsResult,
                DomainError>(
                    CatalogImportErrors
                        .CurrentUserNotFound());
        }

        if (!user.CanEditCatalogImport())
        {
            return Result.Failure<
                CatalogImportBatchDetailsResult,
                DomainError>(
                    CatalogImportErrors
                        .UserCannotAccessBatch());
        }

        var batch =
            await _previewReader
                .GetBatchAsync(
                    batchId,
                    cancellationToken)
                .ConfigureAwait(false);

        if (batch is null)
        {
            return Result.Failure<
                CatalogImportBatchDetailsResult,
                DomainError>(
                    CatalogImportErrors
                        .BatchNotFound(batchId));
        }

        if (!CanAccessBatch(
                user,
                batch))
        {
            return Result.Failure<
                CatalogImportBatchDetailsResult,
                DomainError>(
                    CatalogImportErrors
                        .UserCannotAccessBatch());
        }

        return Result.Success<
            CatalogImportBatchDetailsResult,
            DomainError>(
                batch);
    }

    private static bool CanAccessBatch(
        User user,
        CatalogImportBatchDetailsResult batch)
    {
        /*
         * Technical видит любой batch.
         * Manager — только созданный им.
         */
        return user.IsTechnical
            || user.IsManager
            && batch.CreatedByUserId
                == user.Id;
    }
}