using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.ImportBatches.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.ImportBatches.DeleteCatalogImportBatch;

public sealed class DeleteCatalogImportBatchCommandHandler
{
    private readonly ICatalogImportBatchRepository _importBatchRepository;
    private readonly IUserRepository _userRepository;

    public DeleteCatalogImportBatchCommandHandler(
        ICatalogImportBatchRepository importBatchRepository,
        IUserRepository userRepository)
    {
        _importBatchRepository = importBatchRepository;
        _userRepository = userRepository;
    }

    public async Task<UnitResult<DomainError>> Handle(
        DeleteCatalogImportBatchCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.CurrentUserId == Guid.Empty)
        {
            return UnitResult.Failure(
                CatalogImportErrors.CurrentUserNotFound());
        }

        if (command.BatchId == Guid.Empty)
        {
            return UnitResult.Failure(
                CatalogImportErrors.BatchNotFound(command.BatchId));
        }

        var currentUser = await _userRepository
            .GetByIdAsync(command.CurrentUserId, cancellationToken)
            .ConfigureAwait(false);

        if (currentUser is null)
        {
            return UnitResult.Failure(
                CatalogImportErrors.CurrentUserNotFound());
        }

        if (!currentUser.CanDeleteOwnCatalogImport())
        {
            return UnitResult.Failure(
                CatalogImportErrors.UserCannotDeleteCatalogImport());
        }

        var batch = await _importBatchRepository
            .GetByIdAsync(command.BatchId, cancellationToken)
            .ConfigureAwait(false);

        if (batch is null)
        {
            return UnitResult.Failure(
                CatalogImportErrors.BatchNotFound(command.BatchId));
        }

        if (batch.CreatedByUserId != currentUser.Id)
        {
            return UnitResult.Failure(
                CatalogImportErrors.UserCannotAccessBatch());
        }

        if (!batch.CanBeDeletedByOwner)
        {
            return UnitResult.Failure(
                CatalogImportErrors.BatchCannotBeDeleted(batch.Status));
        }

        _importBatchRepository.Remove(batch);

        var saved = await _importBatchRepository
            .TrySaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!saved)
        {
            return UnitResult.Failure(
                CatalogImportErrors.BatchConcurrencyConflict());
        }

        return UnitResult.Success<DomainError>();
    }
}