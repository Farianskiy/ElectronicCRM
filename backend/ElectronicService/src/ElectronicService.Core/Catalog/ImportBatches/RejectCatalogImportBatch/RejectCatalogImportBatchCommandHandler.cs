using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.ImportBatches.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.ImportBatches.RejectCatalogImportBatch;

public sealed class RejectCatalogImportBatchCommandHandler
{
    private readonly ICatalogImportBatchRepository _importBatchRepository;
    private readonly IUserRepository _userRepository;

    public RejectCatalogImportBatchCommandHandler(
        ICatalogImportBatchRepository importBatchRepository,
        IUserRepository userRepository)
    {
        _importBatchRepository = importBatchRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<RejectCatalogImportBatchResult, DomainError>> Handle(
        RejectCatalogImportBatchCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.CurrentUserId == Guid.Empty)
        {
            return Result.Failure<RejectCatalogImportBatchResult, DomainError>(
                CatalogImportErrors.CurrentUserNotFound());
        }

        if (command.BatchId == Guid.Empty)
        {
            return Result.Failure<RejectCatalogImportBatchResult, DomainError>(
                CatalogImportErrors.BatchNotFound(command.BatchId));
        }

        var currentUser = await _userRepository
            .GetByIdAsync(command.CurrentUserId, cancellationToken)
            .ConfigureAwait(false);

        if (currentUser is null)
        {
            return Result.Failure<RejectCatalogImportBatchResult, DomainError>(
                CatalogImportErrors.CurrentUserNotFound());
        }

        if (!currentUser.CanReviewCatalogImports())
        {
            return Result.Failure<RejectCatalogImportBatchResult, DomainError>(
                CatalogImportErrors.UserCannotReviewCatalogImports());
        }

        var batch = await _importBatchRepository
            .GetByIdAsync(command.BatchId, cancellationToken)
            .ConfigureAwait(false);

        if (batch is null)
        {
            return Result.Failure<RejectCatalogImportBatchResult, DomainError>(
                CatalogImportErrors.BatchNotFound(command.BatchId));
        }

        var rejectResult = batch.Reject(
            currentUser.Id,
            command.Reason ?? string.Empty);

        if (rejectResult.IsFailure)
        {
            return Result.Failure<RejectCatalogImportBatchResult, DomainError>(
                rejectResult.Error);
        }

        var saved = await _importBatchRepository
            .TrySaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!saved)
        {
            return Result.Failure<RejectCatalogImportBatchResult, DomainError>(
                CatalogImportErrors.BatchConcurrencyConflict());
        }

        var result = new RejectCatalogImportBatchResult(
            batch.Id,
            batch.Status,
            batch.RejectedByUserId,
            batch.RejectedAtUtc,
            batch.RejectionReason,
            batch.Version);

        return Result.Success<RejectCatalogImportBatchResult, DomainError>(result);
    }
}