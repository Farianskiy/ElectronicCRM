using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.ImportBatches.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Common;
using ElectronicService.Domain.Catalog.ImportBatches;

namespace ElectronicService.Core.Catalog.ImportBatches.SubmitCatalogImportBatch;

public sealed class SubmitCatalogImportBatchCommandHandler
{
    private readonly ICatalogImportBatchRepository _importBatchRepository;
    private readonly IUserRepository _userRepository;

    public SubmitCatalogImportBatchCommandHandler(
        ICatalogImportBatchRepository importBatchRepository,
        IUserRepository userRepository)
    {
        _importBatchRepository = importBatchRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<SubmitCatalogImportBatchResult, DomainError>> Handle(
        SubmitCatalogImportBatchCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.CurrentUserId == Guid.Empty)
        {
            return Result.Failure<SubmitCatalogImportBatchResult, DomainError>(
                CatalogImportErrors.CurrentUserNotFound());
        }

        if (command.BatchId == Guid.Empty)
        {
            return Result.Failure<SubmitCatalogImportBatchResult, DomainError>(
                CatalogImportErrors.BatchNotFound(command.BatchId));
        }

        var currentUser = await _userRepository
            .GetByIdAsync(command.CurrentUserId, cancellationToken)
            .ConfigureAwait(false);

        if (currentUser is null)
        {
            return Result.Failure<SubmitCatalogImportBatchResult, DomainError>(
                CatalogImportErrors.CurrentUserNotFound());
        }

        if (!currentUser.CanSubmitCatalogImportForReview())
        {
            return Result.Failure<SubmitCatalogImportBatchResult, DomainError>(
                CatalogImportErrors.UserCannotSubmitCatalogImport());
        }

        var batch = await _importBatchRepository
            .GetByIdAsync(command.BatchId, cancellationToken)
            .ConfigureAwait(false);

        if (batch is null)
        {
            return Result.Failure<SubmitCatalogImportBatchResult, DomainError>(
                CatalogImportErrors.BatchNotFound(command.BatchId));
        }

        if (batch.CreatedByUserId != currentUser.Id)
        {
            return Result.Failure<SubmitCatalogImportBatchResult, DomainError>(
                CatalogImportErrors.UserCannotAccessBatch());
        }

        var submitResult = batch.SubmitForReview();

        if (submitResult.IsFailure)
        {
            return Result.Failure<SubmitCatalogImportBatchResult, DomainError>(
                submitResult.Error);
        }

        await _importBatchRepository
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        var result = new SubmitCatalogImportBatchResult(
            batch.Id,
            batch.Status,
            batch.SubmittedAtUtc,
            batch.Version);

        return Result.Success<SubmitCatalogImportBatchResult, DomainError>(result);
    }
}