using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog
    .ImportBatches.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog
    .ImportBatches;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog
    .ImportBatches.GetCatalogImportBatch;

public sealed class
    GetCatalogImportBatchQueryHandler
{
    private readonly ICatalogImportBatchRepository
        _importBatchRepository;

    private readonly IUserRepository
        _userRepository;

    public GetCatalogImportBatchQueryHandler(
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
        GetCatalogImportBatchResult,
        DomainError>> Handle(
            GetCatalogImportBatchQuery query,
            CancellationToken cancellationToken =
                default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.CurrentUserId == Guid.Empty)
        {
            return Result.Failure<
                GetCatalogImportBatchResult,
                DomainError>(
                    CatalogImportErrors
                        .CurrentUserNotFound());
        }

        if (query.BatchId == Guid.Empty)
        {
            return Result.Failure<
                GetCatalogImportBatchResult,
                DomainError>(
                    CatalogImportErrors
                        .BatchNotFound(
                            query.BatchId));
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
                GetCatalogImportBatchResult,
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
                GetCatalogImportBatchResult,
                DomainError>(
                    CatalogImportErrors
                        .BatchNotFound(
                            query.BatchId));
        }

        var isOwner =
            batch.CreatedByUserId
            == currentUser.Id;

        /*
         * Владелец видит собственный пакет.
         *
         * Technical может видеть чужие пакеты,
         * потому что он будет их проверять.
         */
        var canRead =
            isOwner
            || currentUser
                .CanReviewCatalogImports();

        if (!canRead)
        {
            return Result.Failure<
                GetCatalogImportBatchResult,
                DomainError>(
                    CatalogImportErrors
                        .UserCannotAccessBatch());
        }

        /*
         * Редактировать пакет пока может
         * только его владелец.
         */
        var canEdit =
            isOwner
            && currentUser
                .CanEditCatalogImport()
            && batch.IsEditable;

        /*
         * Regular и Manager отправляют
         * готовый пакет на проверку.
         */
        var canSubmit =
            isOwner
            && currentUser
                .CanSubmitCatalogImportForReview()
            && batch.Status
                == CatalogImportBatchStatus.Ready;

        /*
         * Technical может:
         *
         * 1. Применить собственный Ready-пакет.
         * 2. Применить чужой пакет,
         *    находящийся UnderReview.
         */
        var canApplyOwnBatch =
            isOwner
            && batch.Status
                == CatalogImportBatchStatus.Ready;

        var canApplyReviewedBatch =
            batch.Status
                == CatalogImportBatchStatus.UnderReview;

        var canApply =
            currentUser
                .CanApplyCatalogImport()
            && (
                canApplyOwnBatch
                || canApplyReviewedBatch
            );

        var result =
            new GetCatalogImportBatchResult(
                batch.Id,
                batch.CreatedByUserId,
                batch.ProductTypeId,
                batch.OriginalFileName,
                batch.FileSizeBytes,
                batch.Status,
                batch.RowsCount,
                batch.ValidRowsCount,
                batch.ErrorRowsCount,
                batch.CreatedAtUtc,
                batch.UpdatedAtUtc,
                batch.SubmittedAtUtc,
                batch.Version,
                canEdit,
                canSubmit,
                canApply);

        return Result.Success<
            GetCatalogImportBatchResult,
            DomainError>(result);
    }
}