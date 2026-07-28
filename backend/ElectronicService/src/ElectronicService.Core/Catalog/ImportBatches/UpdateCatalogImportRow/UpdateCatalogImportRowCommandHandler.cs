using System.Text.Json;
using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.ImportBatches.Abstractions;
using ElectronicService.Core.Catalog.ImportBatches.Analysis;
using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.ImportBatches.UpdateCatalogImportRow;

public sealed class UpdateCatalogImportRowCommandHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ICatalogImportBatchRepository _importBatchRepository;
    private readonly ICatalogProductMetadataRepository _metadataRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICatalogImportRowValidator _rowValidator;

    public UpdateCatalogImportRowCommandHandler(
        ICatalogImportBatchRepository importBatchRepository,
        ICatalogProductMetadataRepository metadataRepository,
        IUserRepository userRepository,
        ICatalogImportRowValidator rowValidator)
    {
        _importBatchRepository = importBatchRepository;
        _metadataRepository = metadataRepository;
        _userRepository = userRepository;
        _rowValidator = rowValidator;
    }

    public async Task<Result<UpdateCatalogImportRowResult, DomainError>> Handle(
        UpdateCatalogImportRowCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.CurrentUserId == Guid.Empty)
        {
            return Result.Failure<UpdateCatalogImportRowResult, DomainError>(
                CatalogImportErrors.CurrentUserNotFound());
        }

        if (command.BatchId == Guid.Empty)
        {
            return Result.Failure<UpdateCatalogImportRowResult, DomainError>(
                CatalogImportErrors.BatchNotFound(command.BatchId));
        }

        if (command.RowId == Guid.Empty)
        {
            return Result.Failure<UpdateCatalogImportRowResult, DomainError>(
                CatalogImportErrors.RowNotFound(command.RowId));
        }

        var currentUser = await _userRepository
            .GetByIdAsync(command.CurrentUserId, cancellationToken)
            .ConfigureAwait(false);

        if (currentUser is null)
        {
            return Result.Failure<UpdateCatalogImportRowResult, DomainError>(
                CatalogImportErrors.CurrentUserNotFound());
        }

        if (!currentUser.CanEditCatalogImport())
        {
            return Result.Failure<UpdateCatalogImportRowResult, DomainError>(
                CatalogImportErrors.UserCannotEditCatalogImport());
        }

        var batch = await _importBatchRepository
            .GetByIdAsync(command.BatchId, cancellationToken)
            .ConfigureAwait(false);

        if (batch is null)
        {
            return Result.Failure<UpdateCatalogImportRowResult, DomainError>(
                CatalogImportErrors.BatchNotFound(command.BatchId));
        }

        if (batch.CreatedByUserId != currentUser.Id)
        {
            return Result.Failure<UpdateCatalogImportRowResult, DomainError>(
                CatalogImportErrors.UserCannotAccessBatch());
        }

        if (!batch.CanEditRows)
        {
            return Result.Failure<UpdateCatalogImportRowResult, DomainError>(
                CatalogImportErrors.BatchRowsCannotBeEdited(batch.Status));
        }

        if (batch.ProductTypeId is not Guid productTypeId)
        {
            return Result.Failure<UpdateCatalogImportRowResult, DomainError>(
                CatalogImportErrors.ProductTypeIsRequired());
        }

        var row = await _importBatchRepository
            .GetRowByIdAsync(command.BatchId, command.RowId, cancellationToken)
            .ConfigureAwait(false);

        if (row is null)
        {
            return Result.Failure<UpdateCatalogImportRowResult, DomainError>(
                CatalogImportErrors.RowNotFound(command.RowId));
        }

        var productType = await _metadataRepository
            .GetProductTypeByIdAsync(productTypeId, cancellationToken)
            .ConfigureAwait(false);

        if (productType is null)
        {
            return Result.Failure<UpdateCatalogImportRowResult, DomainError>(
                CatalogImportErrors.ProductTypeNotFound(productTypeId));
        }

        var characteristicDefinitionIds = productType.Characteristics
            .Select(characteristic => characteristic.CharacteristicDefinitionId)
            .ToArray();

        var characteristicDefinitions = await _metadataRepository
            .GetCharacteristicDefinitionsByIdsAsync(
                characteristicDefinitionIds,
                cancellationToken)
            .ConfigureAwait(false);

        string? manufacturerName = null;

        if (command.ManufacturerId is Guid manufacturerId)
        {
            var manufacturer = await _metadataRepository
                .GetManufacturerByIdAsync(manufacturerId, cancellationToken)
                .ConfigureAwait(false);

            if (manufacturer is null)
            {
                return Result.Failure<UpdateCatalogImportRowResult, DomainError>(
                    CatalogImportErrors.ManufacturerNotFound(manufacturerId));
            }

            manufacturerName = manufacturer.Name;
        }

        var data = new CatalogImportNormalizedRowData(
            command.Name,
            command.Article,
            manufacturerName,
            command.Price,
            command.StockQuantity,
            command.Characteristics,
            command.ManufacturerId);

        var validationResult = _rowValidator.Validate(
            data,
            productType,
            characteristicDefinitions);

        var normalizedDataJson = JsonSerializer.Serialize(
            validationResult.Data,
            JsonOptions);

        var issuesJson = JsonSerializer.Serialize(
            validationResult.Issues,
            JsonOptions);

        var warningsJson = JsonSerializer.Serialize(
            validationResult.Warnings,
            JsonOptions);

        var oldRowStatus = row.Status;

        var replaceResult = row.ReplaceValidationResult(
            validationResult.Status,
            normalizedDataJson,
            issuesJson,
            warningsJson);

        if (replaceResult.IsFailure)
        {
            return Result.Failure<UpdateCatalogImportRowResult, DomainError>(
                replaceResult.Error);
        }

        var validRowsCount = batch.ValidRowsCount;
        var errorRowsCount = batch.ErrorRowsCount;

        if (oldRowStatus == CatalogImportRowStatus.Valid)
        {
            validRowsCount--;
        }
        else if (oldRowStatus == CatalogImportRowStatus.Error)
        {
            errorRowsCount--;
        }

        if (validationResult.Status == CatalogImportRowStatus.Valid)
        {
            validRowsCount++;
        }
        else if (validationResult.Status == CatalogImportRowStatus.Error)
        {
            errorRowsCount++;
        }

        var statisticsResult = batch.RefreshRowsValidationStatistics(
            batch.RowsCount,
            validRowsCount,
            errorRowsCount);

        if (statisticsResult.IsFailure)
        {
            return Result.Failure<UpdateCatalogImportRowResult, DomainError>(
                statisticsResult.Error);
        }

        await _importBatchRepository
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        var result = new UpdateCatalogImportRowResult(
            row.Id,
            row.RowNumber,
            row.Status,
            validationResult.Data,
            validationResult.Issues,
            validationResult.Warnings,
            batch.Status,
            batch.RowsCount,
            batch.ValidRowsCount,
            batch.ErrorRowsCount,
            batch.Version);

        return Result.Success<UpdateCatalogImportRowResult, DomainError>(result);
    }
}