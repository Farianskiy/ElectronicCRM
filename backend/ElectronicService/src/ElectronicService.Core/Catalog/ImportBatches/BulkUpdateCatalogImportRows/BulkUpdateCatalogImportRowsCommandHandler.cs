using System.Text.Json;
using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.ImportBatches.Abstractions;
using ElectronicService.Core.Catalog.ImportBatches.Analysis;
using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.ImportBatches.BulkUpdateCatalogImportRows;

public sealed class BulkUpdateCatalogImportRowsCommandHandler
{
    public const int MaximumRowsPerRequest = 200;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ICatalogImportBatchRepository _importBatchRepository;
    private readonly ICatalogProductMetadataRepository _metadataRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICatalogImportRowValidator _rowValidator;
    private readonly ICatalogImportRecognitionFeedbackCollector _recognitionFeedbackCollector;

    public BulkUpdateCatalogImportRowsCommandHandler(
        ICatalogImportBatchRepository importBatchRepository,
        ICatalogProductMetadataRepository metadataRepository,
        IUserRepository userRepository,
        ICatalogImportRowValidator rowValidator,
        ICatalogImportRecognitionFeedbackCollector recognitionFeedbackCollector)
    {
        ArgumentNullException.ThrowIfNull(importBatchRepository);
        ArgumentNullException.ThrowIfNull(metadataRepository);
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(rowValidator);
        ArgumentNullException.ThrowIfNull(recognitionFeedbackCollector);

        _importBatchRepository = importBatchRepository;
        _metadataRepository = metadataRepository;
        _userRepository = userRepository;
        _rowValidator = rowValidator;
        _recognitionFeedbackCollector = recognitionFeedbackCollector;
    }

    public async Task<Result<BulkUpdateCatalogImportRowsResult, DomainError>> Handle(BulkUpdateCatalogImportRowsCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.CurrentUserId == Guid.Empty)
        {
            return Result.Failure<BulkUpdateCatalogImportRowsResult, DomainError>(
                CatalogImportErrors.CurrentUserNotFound());
        }

        if (command.BatchId == Guid.Empty)
        {
            return Result.Failure<BulkUpdateCatalogImportRowsResult, DomainError>(
                CatalogImportErrors.BatchNotFound(command.BatchId));
        }

        if (command.Rows.Count == 0)
        {
            return Result.Failure<BulkUpdateCatalogImportRowsResult, DomainError>(
                CatalogImportErrors.BulkUpdateRowsAreRequired());
        }

        if (command.Rows.Count > MaximumRowsPerRequest)
        {
            return Result.Failure<BulkUpdateCatalogImportRowsResult, DomainError>(
                CatalogImportErrors.BulkUpdateRowsLimitExceeded(MaximumRowsPerRequest));
        }

        var rowIds = command.Rows
            .Select(row => row.RowId)
            .ToArray();

        var emptyRowIdExists = rowIds.Any(rowId => rowId == Guid.Empty);

        if (emptyRowIdExists)
        {
            return Result.Failure<BulkUpdateCatalogImportRowsResult, DomainError>(
                CatalogImportErrors.RowNotFound(Guid.Empty));
        }

        var duplicateRowId = rowIds
            .GroupBy(rowId => rowId)
            .Where(group => group.Count() > 1)
            .Select(group => (Guid?)group.Key)
            .FirstOrDefault();

        if (duplicateRowId.HasValue)
        {
            return Result.Failure<BulkUpdateCatalogImportRowsResult, DomainError>(
                CatalogImportErrors.DuplicateBulkUpdateRow(duplicateRowId.Value));
        }

        var currentUser = await _userRepository
            .GetByIdAsync(command.CurrentUserId, cancellationToken)
            .ConfigureAwait(false);

        if (currentUser is null)
        {
            return Result.Failure<BulkUpdateCatalogImportRowsResult, DomainError>(
                CatalogImportErrors.CurrentUserNotFound());
        }

        if (!currentUser.CanEditCatalogImport())
        {
            return Result.Failure<BulkUpdateCatalogImportRowsResult, DomainError>(
                CatalogImportErrors.UserCannotEditCatalogImport());
        }

        var batch = await _importBatchRepository
            .GetByIdAsync(command.BatchId, cancellationToken)
            .ConfigureAwait(false);

        if (batch is null)
        {
            return Result.Failure<BulkUpdateCatalogImportRowsResult, DomainError>(
                CatalogImportErrors.BatchNotFound(command.BatchId));
        }

        if (batch.CreatedByUserId != currentUser.Id)
        {
            return Result.Failure<BulkUpdateCatalogImportRowsResult, DomainError>(
                CatalogImportErrors.UserCannotAccessBatch());
        }

        if (!batch.CanEditRows)
        {
            return Result.Failure<BulkUpdateCatalogImportRowsResult, DomainError>(
                CatalogImportErrors.BatchRowsCannotBeEdited(batch.Status));
        }

        if (batch.Version != command.ExpectedVersion)
        {
            return Result.Failure<BulkUpdateCatalogImportRowsResult, DomainError>(
                CatalogImportErrors.BatchConcurrencyConflict());
        }

        if (batch.ProductTypeId is not Guid productTypeId)
        {
            return Result.Failure<BulkUpdateCatalogImportRowsResult, DomainError>(
                CatalogImportErrors.ProductTypeIsRequired());
        }

        var rows = await _importBatchRepository
            .GetRowsByIdsAsync(command.BatchId, rowIds, cancellationToken)
            .ConfigureAwait(false);

        var rowsById = rows.ToDictionary(row => row.Id);

        var missingRowId = rowIds.FirstOrDefault(rowId => !rowsById.ContainsKey(rowId));

        if (missingRowId != Guid.Empty)
        {
            return Result.Failure<BulkUpdateCatalogImportRowsResult, DomainError>(
                CatalogImportErrors.RowNotFound(missingRowId));
        }

        var previousDataByRowId = new Dictionary<Guid, CatalogImportNormalizedRowData>();

        foreach (var row in rows)
        {
            var previousData = DeserializeNormalizedData(row.NormalizedDataJson);

            if (previousData is null)
            {
                return Result.Failure<BulkUpdateCatalogImportRowsResult, DomainError>(
                    CatalogImportErrors.InvalidImportJson(nameof(CatalogImportRow.NormalizedDataJson)));
            }

            previousDataByRowId.Add(row.Id, previousData);
        }

        var productType = await _metadataRepository
                    .GetProductTypeByIdAsync(productTypeId, cancellationToken)
            .ConfigureAwait(false);

        if (productType is null)
        {
            return Result.Failure<BulkUpdateCatalogImportRowsResult, DomainError>(
                CatalogImportErrors.ProductTypeNotFound(productTypeId));
        }

        var characteristicDefinitionIds = productType.Characteristics
            .Select(characteristic => characteristic.CharacteristicDefinitionId)
            .ToArray();

        var characteristicDefinitions = await _metadataRepository
            .GetCharacteristicDefinitionsByIdsAsync(characteristicDefinitionIds, cancellationToken)
            .ConfigureAwait(false);

        var manufacturers = await _metadataRepository
            .GetManufacturersAsync(cancellationToken)
            .ConfigureAwait(false);

        var manufacturerNamesById = manufacturers.ToDictionary(manufacturer => manufacturer.Id, manufacturer => manufacturer.Name);

        var missingManufacturerId = command.Rows
            .Select(row => row.ManufacturerId)
            .FirstOrDefault(manufacturerId => manufacturerId.HasValue && !manufacturerNamesById.ContainsKey(manufacturerId.Value));

        if (missingManufacturerId is Guid manufacturerId)
        {
            return Result.Failure<BulkUpdateCatalogImportRowsResult, DomainError>(
                CatalogImportErrors.ManufacturerNotFound(manufacturerId));
        }

        var validRowsCount = batch.ValidRowsCount;
        var errorRowsCount = batch.ErrorRowsCount;
        var updatedRows = new List<BulkUpdatedCatalogImportRowResult>(command.Rows.Count);

        foreach (var item in command.Rows)
        {
            var row = rowsById[item.RowId];

            string? manufacturerName = null;

            if (item.ManufacturerId is Guid itemManufacturerId)
            {
                manufacturerName = manufacturerNamesById[itemManufacturerId];
            }

            var data = new CatalogImportNormalizedRowData(
                item.Name,
                item.Article,
                manufacturerName,
                item.Price,
                item.StockQuantity,
                item.Characteristics,
                item.ManufacturerId);

            var validationResult = _rowValidator.Validate(
                data,
                productType,
                characteristicDefinitions);

            var feedbackCollectionRequest = new CatalogImportRecognitionFeedbackCollectionRequest(
                batch.Id,
                row.Id,
                productType,
                characteristicDefinitions,
                previousDataByRowId[row.Id],
                validationResult.Data);

            var feedbackCollectionResult = await _recognitionFeedbackCollector
                .CollectAsync(feedbackCollectionRequest, cancellationToken)
                .ConfigureAwait(false);

            if (feedbackCollectionResult.IsFailure)
            {
                return Result.Failure<BulkUpdateCatalogImportRowsResult, DomainError>(
                    feedbackCollectionResult.Error);
            }

            var effectiveData = feedbackCollectionResult.Value.Data;

            var normalizedDataJson = JsonSerializer.Serialize(effectiveData, JsonOptions);
            var issuesJson = JsonSerializer.Serialize(validationResult.Issues, JsonOptions);
            var warningsJson = JsonSerializer.Serialize(validationResult.Warnings, JsonOptions);

            var oldRowStatus = row.Status;

            var replaceResult = row.ReplaceValidationResult(
                validationResult.Status,
                normalizedDataJson,
                issuesJson,
                warningsJson);

            if (replaceResult.IsFailure)
            {
                return Result.Failure<BulkUpdateCatalogImportRowsResult, DomainError>(
                    replaceResult.Error);
            }

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

            updatedRows.Add(
                new BulkUpdatedCatalogImportRowResult(
                    row.Id,
                    row.RowNumber,
                    row.Status,
                    effectiveData,
                    validationResult.Issues,
                    validationResult.Warnings));
        }

        var statisticsResult = batch.RefreshRowsValidationStatistics(
            batch.RowsCount,
            validRowsCount,
            errorRowsCount);

        if (statisticsResult.IsFailure)
        {
            return Result.Failure<BulkUpdateCatalogImportRowsResult, DomainError>(
                statisticsResult.Error);
        }

        var saved = await _importBatchRepository
            .TrySaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!saved)
        {
            return Result.Failure<BulkUpdateCatalogImportRowsResult, DomainError>(
                CatalogImportErrors.BatchConcurrencyConflict());
        }

        var result = new BulkUpdateCatalogImportRowsResult(
            updatedRows,
            batch.Status,
            batch.RowsCount,
            batch.ValidRowsCount,
            batch.ErrorRowsCount,
            batch.Version);

        return Result.Success<BulkUpdateCatalogImportRowsResult, DomainError>(result);
    }

    private static CatalogImportNormalizedRowData? DeserializeNormalizedData(string normalizedDataJson)
    {
        try
        {
            return JsonSerializer.Deserialize<CatalogImportNormalizedRowData>(
                normalizedDataJson,
                JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }
}