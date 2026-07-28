using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.ImportBatches.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.ImportBatches.StartCatalogImportReview;

public sealed class StartCatalogImportReviewCommandHandler
{
    private readonly ICatalogImportBatchRepository _importBatchRepository;
    private readonly IUserRepository _userRepository;

    public StartCatalogImportReviewCommandHandler(
        ICatalogImportBatchRepository importBatchRepository,
        IUserRepository userRepository)
    {
        _importBatchRepository = importBatchRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<StartCatalogImportReviewResult, DomainError>> Handle(
        StartCatalogImportReviewCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.CurrentUserId == Guid.Empty)
        {
            return Result.Failure<StartCatalogImportReviewResult, DomainError>(
                CatalogImportErrors.CurrentUserNotFound());
        }

        if (command.BatchId == Guid.Empty)
        {
            return Result.Failure<StartCatalogImportReviewResult, DomainError>(
                CatalogImportErrors.BatchNotFound(command.BatchId));
        }

        var currentUser = await _userRepository
            .GetByIdAsync(command.CurrentUserId, cancellationToken)
            .ConfigureAwait(false);

        if (currentUser is null)
        {
            return Result.Failure<StartCatalogImportReviewResult, DomainError>(
                CatalogImportErrors.CurrentUserNotFound());
        }

        if (!currentUser.CanReviewCatalogImports())
        {
            return Result.Failure<StartCatalogImportReviewResult, DomainError>(
                CatalogImportErrors.UserCannotReviewCatalogImports());
        }

        var batch = await _importBatchRepository
            .GetByIdAsync(command.BatchId, cancellationToken)
            .ConfigureAwait(false);

        if (batch is null)
        {
            return Result.Failure<StartCatalogImportReviewResult, DomainError>(
                CatalogImportErrors.BatchNotFound(command.BatchId));
        }

        var startReviewResult = batch.StartReview(currentUser.Id);

        if (startReviewResult.IsFailure)
        {
            return Result.Failure<StartCatalogImportReviewResult, DomainError>(
                startReviewResult.Error);
        }

        var saved = await _importBatchRepository
            .TrySaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!saved)
        {
            return Result.Failure<StartCatalogImportReviewResult, DomainError>(
                CatalogImportErrors.BatchConcurrencyConflict());
        }

        var result = new StartCatalogImportReviewResult(
            batch.Id,
            batch.Status,
            batch.ReviewedByUserId,
            batch.ReviewedAtUtc,
            batch.Version);

        return Result.Success<StartCatalogImportReviewResult, DomainError>(result);
    }
}