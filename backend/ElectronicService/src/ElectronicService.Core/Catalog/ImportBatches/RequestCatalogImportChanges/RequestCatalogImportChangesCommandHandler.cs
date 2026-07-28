using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.ImportBatches.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.ImportBatches.RequestCatalogImportChanges;

public sealed class RequestCatalogImportChangesCommandHandler
{
    private readonly ICatalogImportBatchRepository _importBatchRepository;
    private readonly IUserRepository _userRepository;

    public RequestCatalogImportChangesCommandHandler(
        ICatalogImportBatchRepository importBatchRepository,
        IUserRepository userRepository)
    {
        _importBatchRepository = importBatchRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<RequestCatalogImportChangesResult, DomainError>> Handle(
        RequestCatalogImportChangesCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.CurrentUserId == Guid.Empty)
        {
            return Result.Failure<RequestCatalogImportChangesResult, DomainError>(
                CatalogImportErrors.CurrentUserNotFound());
        }

        if (command.BatchId == Guid.Empty)
        {
            return Result.Failure<RequestCatalogImportChangesResult, DomainError>(
                CatalogImportErrors.BatchNotFound(command.BatchId));
        }

        var currentUser = await _userRepository
            .GetByIdAsync(command.CurrentUserId, cancellationToken)
            .ConfigureAwait(false);

        if (currentUser is null)
        {
            return Result.Failure<RequestCatalogImportChangesResult, DomainError>(
                CatalogImportErrors.CurrentUserNotFound());
        }

        if (!currentUser.CanReviewCatalogImports())
        {
            return Result.Failure<RequestCatalogImportChangesResult, DomainError>(
                CatalogImportErrors.UserCannotReviewCatalogImports());
        }

        var batch = await _importBatchRepository
            .GetByIdAsync(command.BatchId, cancellationToken)
            .ConfigureAwait(false);

        if (batch is null)
        {
            return Result.Failure<RequestCatalogImportChangesResult, DomainError>(
                CatalogImportErrors.BatchNotFound(command.BatchId));
        }

        var requestChangesResult = batch.RequestChanges(
            currentUser.Id,
            command.Comment ?? string.Empty);

        if (requestChangesResult.IsFailure)
        {
            return Result.Failure<RequestCatalogImportChangesResult, DomainError>(
                requestChangesResult.Error);
        }

        var saved = await _importBatchRepository
            .TrySaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!saved)
        {
            return Result.Failure<RequestCatalogImportChangesResult, DomainError>(
                CatalogImportErrors.BatchConcurrencyConflict());
        }

        var result = new RequestCatalogImportChangesResult(
            batch.Id,
            batch.Status,
            batch.ChangesRequestedByUserId,
            batch.ChangesRequestedAtUtc,
            batch.ChangesRequestComment,
            batch.Version);

        return Result.Success<RequestCatalogImportChangesResult, DomainError>(result);
    }
}