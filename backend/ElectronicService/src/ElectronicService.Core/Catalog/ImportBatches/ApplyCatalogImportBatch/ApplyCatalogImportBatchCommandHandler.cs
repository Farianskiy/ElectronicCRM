using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.ImportBatches.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Domain.Common;
using ElectronicService.Domain.Users.Enums;

namespace ElectronicService.Core.Catalog.ImportBatches.ApplyCatalogImportBatch;

public sealed class ApplyCatalogImportBatchCommandHandler
{
    private readonly ICatalogImportBatchRepository _importBatchRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICatalogImportBatchApplier _batchApplier;

    public ApplyCatalogImportBatchCommandHandler(
        ICatalogImportBatchRepository importBatchRepository,
        IUserRepository userRepository,
        ICatalogImportBatchApplier batchApplier)
    {
        _importBatchRepository = importBatchRepository;
        _userRepository = userRepository;
        _batchApplier = batchApplier;
    }

    public async Task<Result<ApplyCatalogImportBatchResult, DomainError>> Handle(
        ApplyCatalogImportBatchCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.CurrentUserId == Guid.Empty)
        {
            return Result.Failure<ApplyCatalogImportBatchResult, DomainError>(
                CatalogImportErrors.CurrentUserNotFound());
        }

        if (command.BatchId == Guid.Empty)
        {
            return Result.Failure<ApplyCatalogImportBatchResult, DomainError>(
                CatalogImportErrors.BatchNotFound(command.BatchId));
        }

        var currentUser = await _userRepository
            .GetByIdAsync(command.CurrentUserId, cancellationToken)
            .ConfigureAwait(false);

        if (currentUser is null)
        {
            return Result.Failure<ApplyCatalogImportBatchResult, DomainError>(
                CatalogImportErrors.CurrentUserNotFound());
        }

        var canReviewImports = await _userRepository
            .HasPermissionAsync(
                currentUser,
                UserPermissionCode.CatalogImportsReview,
                cancellationToken)
            .ConfigureAwait(false);

        if (!canReviewImports)
        {
            return Result.Failure<ApplyCatalogImportBatchResult, DomainError>(
                CatalogImportErrors.UserCannotApplyCatalogImport());
        }

        var batch = await _importBatchRepository
            .GetByIdAsync(command.BatchId, cancellationToken)
            .ConfigureAwait(false);

        if (batch is null)
        {
            return Result.Failure<ApplyCatalogImportBatchResult, DomainError>(
                CatalogImportErrors.BatchNotFound(command.BatchId));
        }

        var canApplyOwnReadyBatch =
            batch.Status == CatalogImportBatchStatus.Ready
            && batch.CreatedByUserId == currentUser.Id;

        var canApplyReviewedBatch =
            batch.Status == CatalogImportBatchStatus.UnderReview
            && batch.ReviewedByUserId == currentUser.Id;

        if (!canApplyOwnReadyBatch && !canApplyReviewedBatch)
        {
            return Result.Failure<ApplyCatalogImportBatchResult, DomainError>(
                CatalogImportErrors.BatchCannotBeAppliedByCurrentUser());
        }

        var applyResult = await _batchApplier.ApplyAsync(batch, currentUser.Id, currentUser.Type, cancellationToken).ConfigureAwait(false);

        if (applyResult.IsFailure)
        {
            return Result.Failure<ApplyCatalogImportBatchResult, DomainError>(
                applyResult.Error);
        }

        var result = new ApplyCatalogImportBatchResult(
            batch.Id,
            batch.Status,
            batch.AppliedByUserId,
            batch.AppliedAtUtc,
            applyResult.Value.CreatedProductsCount,
            applyResult.Value.UpdatedProductsCount,
            batch.Version);

        return Result.Success<ApplyCatalogImportBatchResult, DomainError>(result);
    }
}
