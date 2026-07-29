using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.ImportBatches.Abstractions;
using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.ImportBatches.UpdateCatalogImportMapping;

public sealed class UpdateCatalogImportMappingCommandHandler
{
    private readonly ICatalogImportBatchRepository _importBatchRepository;
    private readonly ICatalogProductMetadataRepository _metadataRepository;
    private readonly IUserRepository _userRepository;

    public UpdateCatalogImportMappingCommandHandler(
        ICatalogImportBatchRepository importBatchRepository,
        ICatalogProductMetadataRepository metadataRepository,
        IUserRepository userRepository)
    {
        _importBatchRepository = importBatchRepository;
        _metadataRepository = metadataRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<UpdateCatalogImportMappingResult, DomainError>> Handle(
        UpdateCatalogImportMappingCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.CurrentUserId == Guid.Empty)
        {
            return Result.Failure<UpdateCatalogImportMappingResult, DomainError>(
                CatalogImportErrors.CurrentUserNotFound());
        }

        if (command.BatchId == Guid.Empty)
        {
            return Result.Failure<UpdateCatalogImportMappingResult, DomainError>(
                CatalogImportErrors.BatchNotFound(command.BatchId));
        }

        if (command.ProductTypeId == Guid.Empty)
        {
            return Result.Failure<UpdateCatalogImportMappingResult, DomainError>(
                CatalogImportErrors.ProductTypeIsRequired());
        }

        if (command.Columns.Count == 0)
        {
            return Result.Failure<UpdateCatalogImportMappingResult, DomainError>(
                CatalogImportErrors.MappingColumnsAreRequired());
        }

        var currentUser = await _userRepository
            .GetByIdAsync(command.CurrentUserId, cancellationToken)
            .ConfigureAwait(false);

        if (currentUser is null)
        {
            return Result.Failure<UpdateCatalogImportMappingResult, DomainError>(
                CatalogImportErrors.CurrentUserNotFound());
        }

        if (!currentUser.CanEditCatalogImport())
        {
            return Result.Failure<UpdateCatalogImportMappingResult, DomainError>(
                CatalogImportErrors.UserCannotEditCatalogImport());
        }

        var batch = await _importBatchRepository
            .GetByIdAsync(command.BatchId, cancellationToken)
            .ConfigureAwait(false);

        if (batch is null)
        {
            return Result.Failure<UpdateCatalogImportMappingResult, DomainError>(
                CatalogImportErrors.BatchNotFound(command.BatchId));
        }

        if (batch.CreatedByUserId != currentUser.Id)
        {
            return Result.Failure<UpdateCatalogImportMappingResult, DomainError>(
                CatalogImportErrors.UserCannotAccessBatch());
        }

        if (!batch.IsEditable)
        {
            return Result.Failure<UpdateCatalogImportMappingResult, DomainError>(
                CatalogImportErrors.BatchMappingCannotBeEdited(batch.Status));
        }

        var productType = await _metadataRepository
            .GetProductTypeByIdAsync(command.ProductTypeId, cancellationToken)
            .ConfigureAwait(false);

        if (productType is null)
        {
            return Result.Failure<UpdateCatalogImportMappingResult, DomainError>(
                CatalogImportErrors.ProductTypeNotFound(command.ProductTypeId));
        }

        var storedColumns = await _importBatchRepository
            .GetColumnsForUpdateAsync(batch.Id, cancellationToken)
            .ConfigureAwait(false);

        if (storedColumns.Count == 0)
        {
            return Result.Failure<UpdateCatalogImportMappingResult, DomainError>(
                CatalogImportErrors.MappingColumnsAreRequired());
        }

        var requestColumnIds = command.Columns
            .Select(mapping => mapping.ColumnId)
            .ToHashSet();

        var containsInvalidColumnId = command.Columns
            .Any(mapping => mapping.ColumnId == Guid.Empty);

        var containsDuplicateColumnId =
            requestColumnIds.Count != command.Columns.Count;

        var columnSetDoesNotMatch =
            storedColumns.Count != requestColumnIds.Count
            || storedColumns.Any(column => !requestColumnIds.Contains(column.Id));

        if (containsInvalidColumnId
            || containsDuplicateColumnId
            || columnSetDoesNotMatch)
        {
            return Result.Failure<UpdateCatalogImportMappingResult, DomainError>(
                CatalogImportErrors.MappingColumnSetMismatch());
        }

        var invalidMapping = command.Columns.FirstOrDefault(mapping =>
            mapping.TargetKind == CatalogImportColumnTargetKind.None
            || (
                mapping.TargetKind == CatalogImportColumnTargetKind.Characteristic
                && (
                    mapping.CharacteristicDefinitionId is null
                    || mapping.CharacteristicDefinitionId == Guid.Empty
                )
            )
            || (
                mapping.TargetKind != CatalogImportColumnTargetKind.Characteristic
                && mapping.CharacteristicDefinitionId is not null
            ));

        if (invalidMapping is not null)
        {
            return Result.Failure<UpdateCatalogImportMappingResult, DomainError>(
                CatalogImportErrors.InvalidColumnMapping());
        }

        var duplicateStandardTarget = command.Columns
            .Where(mapping => RequiresUniqueTarget(mapping.TargetKind))
            .GroupBy(mapping => mapping.TargetKind)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateStandardTarget is not null)
        {
            return Result.Failure<UpdateCatalogImportMappingResult, DomainError>(
                CatalogImportErrors.DuplicateColumnMapping());
        }

        var duplicateCharacteristicId = command.Columns
            .Where(mapping =>
                mapping.TargetKind == CatalogImportColumnTargetKind.Characteristic
                && mapping.CharacteristicDefinitionId.HasValue)
            .GroupBy(mapping => mapping.CharacteristicDefinitionId!.Value)
            .Where(group => group.Count() > 1)
            .Select(group => (Guid?)group.Key)
            .FirstOrDefault();

        if (duplicateCharacteristicId.HasValue)
        {
            return Result.Failure<UpdateCatalogImportMappingResult, DomainError>(
                CatalogImportErrors.DuplicateColumnMapping());
        }

        var disallowedCharacteristic = command.Columns.FirstOrDefault(mapping =>
            mapping.TargetKind == CatalogImportColumnTargetKind.Characteristic
            && mapping.CharacteristicDefinitionId.HasValue
            && !productType.AllowsCharacteristic(
                mapping.CharacteristicDefinitionId.Value));

        if (disallowedCharacteristic is not null)
        {
            return Result.Failure<UpdateCatalogImportMappingResult, DomainError>(
                CatalogImportErrors.CharacteristicNotAllowed(
                    disallowedCharacteristic.CharacteristicDefinitionId!.Value,
                    productType.Id));
        }

        var assignProductTypeResult = batch.AssignProductType(productType.Id);

        if (assignProductTypeResult.IsFailure)
        {
            return Result.Failure<UpdateCatalogImportMappingResult, DomainError>(
                assignProductTypeResult.Error);
        }

        var mappingsByColumnId = command.Columns.ToDictionary(
            mapping => mapping.ColumnId);

        foreach (var column in storedColumns)
        {
            var mapping = mappingsByColumnId[column.Id];

            var isConfirmed =
                mapping.TargetKind != CatalogImportColumnTargetKind.Unmapped;

            var changeMappingResult = column.ChangeMapping(
                mapping.TargetKind,
                mapping.CharacteristicDefinitionId,
                isConfirmed);

            if (changeMappingResult.IsFailure)
            {
                return Result.Failure<UpdateCatalogImportMappingResult, DomainError>(
                    changeMappingResult.Error);
            }
        }

        var markMappingChangedResult = batch.MarkMappingChanged();

        if (markMappingChangedResult.IsFailure)
        {
            return Result.Failure<UpdateCatalogImportMappingResult, DomainError>(
                markMappingChangedResult.Error);
        }

        var saved = await _importBatchRepository
            .TrySaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!saved)
        {
            return Result.Failure<UpdateCatalogImportMappingResult, DomainError>(
                CatalogImportErrors.BatchConcurrencyConflict());
        }

        var result = new UpdateCatalogImportMappingResult(
            batch.Id,
            batch.Status,
            productType.Id,
            storedColumns.Count,
            storedColumns.Count(column =>
                column.TargetKind == CatalogImportColumnTargetKind.Unmapped),
            storedColumns.Count(column => !column.IsConfirmed),
            batch.Version);

        return Result.Success<UpdateCatalogImportMappingResult, DomainError>(
            result);
    }

    private static bool RequiresUniqueTarget(CatalogImportColumnTargetKind targetKind)
    {
        return targetKind is
            CatalogImportColumnTargetKind.Name
            or CatalogImportColumnTargetKind.Article
            or CatalogImportColumnTargetKind.Manufacturer
            or CatalogImportColumnTargetKind.Price
            or CatalogImportColumnTargetKind.StockQuantity;
    }
}