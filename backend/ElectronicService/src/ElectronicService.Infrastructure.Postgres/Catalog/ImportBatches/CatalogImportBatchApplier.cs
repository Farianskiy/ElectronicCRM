using System.Data;
using System.Globalization;
using System.Text.Json;
using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Characteristics.Normalization;
using ElectronicService.Core.Catalog.ImportBatches.Analysis;
using ElectronicService.Core.Catalog.ImportBatches.ApplyCatalogImportBatch;
using ElectronicService.Core.Catalog.Products.Audit;
using ElectronicService.Core.Catalog.Recognition.Learning;
using ElectronicService.Domain.Catalog.Audit;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Domain.Catalog.Products;
using ElectronicService.Domain.Catalog.ValueObjects;
using ElectronicService.Domain.Common;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace ElectronicService.Infrastructure.Postgres.Catalog.ImportBatches;

public sealed class CatalogImportBatchApplier : ICatalogImportBatchApplier
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly Action<ILogger, Guid, Exception?> LogApplyFailed =
    LoggerMessage.Define<Guid>(
        LogLevel.Error,
        new EventId(1, nameof(CatalogImportBatchApplier)),
        "Catalog import batch {BatchId} application failed.");

    private static readonly Action<ILogger, Guid, string, string, Exception?> LogCandidateAggregationRejected =
        LoggerMessage.Define<Guid, string, string>(
            LogLevel.Warning,
            new EventId(2, nameof(CatalogImportBatchApplier)),
            "Catalog recognition candidate aggregation after batch {BatchId} was rejected. Error code: {ErrorCode}. Error message: {ErrorMessage}");

    private static readonly Action<ILogger, Guid, int, int, int, int, Exception?> LogCandidateAggregationCompleted =
        LoggerMessage.Define<Guid, int, int, int, int>(
            LogLevel.Information,
            new EventId(3, nameof(CatalogImportBatchApplier)),
            "Catalog recognition candidate aggregation after batch {BatchId} completed. Scanned feedback: {ScannedFeedbackCount}, created candidates: {CreatedCandidateCount}, updated candidates: {UpdatedCandidateCount}, added evidence: {AddedEvidenceCount}.");

    private static readonly Action<ILogger, Guid, Exception?> LogCandidateAggregationFailed =
        LoggerMessage.Define<Guid>(
            LogLevel.Error,
            new EventId(4, nameof(CatalogImportBatchApplier)),
            "Catalog recognition candidate aggregation after batch {BatchId} failed. The catalog import batch remains successfully applied.");

    private static readonly Action<ILogger, Guid, string, string, Exception?> LogSuggestionPromotionRejected =
    LoggerMessage.Define<Guid, string, string>(
        LogLevel.Warning,
        new EventId(5, nameof(CatalogImportBatchApplier)),
        "Catalog recognition candidate suggestion promotion after batch {BatchId} was rejected. Error code: {ErrorCode}. Error message: {ErrorMessage}");

    private static readonly Action<ILogger, Guid, int, int, int, int, Exception?> LogSuggestionPromotionCompleted =
        LoggerMessage.Define<Guid, int, int, int, int>(
            LogLevel.Information,
            new EventId(6, nameof(CatalogImportBatchApplier)),
            "Catalog recognition candidate suggestion promotion after batch {BatchId} completed. Scanned candidates: {ScannedCandidateCount}, eligible candidates: {EligibleCandidateCount}, created suggestions: {CreatedSuggestionCount}, attached existing suggestions: {AttachedExistingSuggestionCount}.");

    private static readonly Action<ILogger, Guid, Exception?> LogSuggestionPromotionFailed =
        LoggerMessage.Define<Guid>(
            LogLevel.Error,
            new EventId(7, nameof(CatalogImportBatchApplier)),
            "Catalog recognition candidate suggestion promotion after batch {BatchId} failed. The catalog import batch remains successfully applied.");

    private readonly ElectronicDbContext _dbContext;
    private readonly ICatalogImportRecognitionFeedbackFinalizer _recognitionFeedbackFinalizer;
    private readonly ICatalogRecognitionCandidateAggregator _recognitionCandidateAggregator;
    private readonly ICatalogRecognitionCandidateSuggestionPromoter _recognitionCandidateSuggestionPromoter;
    private readonly ILogger<CatalogImportBatchApplier> _logger;

    public CatalogImportBatchApplier(
        ElectronicDbContext dbContext,
        ICatalogImportRecognitionFeedbackFinalizer recognitionFeedbackFinalizer,
        ICatalogRecognitionCandidateAggregator recognitionCandidateAggregator,
        ICatalogRecognitionCandidateSuggestionPromoter recognitionCandidateSuggestionPromoter,
        ILogger<CatalogImportBatchApplier> logger)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(recognitionFeedbackFinalizer);
        ArgumentNullException.ThrowIfNull(recognitionCandidateAggregator);
        ArgumentNullException.ThrowIfNull(recognitionCandidateSuggestionPromoter);
        ArgumentNullException.ThrowIfNull(logger);

        _dbContext = dbContext;
        _recognitionFeedbackFinalizer = recognitionFeedbackFinalizer;
        _recognitionCandidateAggregator = recognitionCandidateAggregator;
        _recognitionCandidateSuggestionPromoter = recognitionCandidateSuggestionPromoter;
        _logger = logger;
    }

    public async Task<Result<CatalogImportApplyExecutionResult, DomainError>> ApplyAsync(
        CatalogImportBatch batch,
        Guid appliedByUserId,
        UserType appliedByUserType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(batch);

        if (appliedByUserId == Guid.Empty)
        {
            return Result.Failure<CatalogImportApplyExecutionResult, DomainError>(
                CatalogImportErrors.CurrentUserNotFound());
        }

        if (appliedByUserType != UserType.Technical)
        {
            return Result.Failure<CatalogImportApplyExecutionResult, DomainError>(
                CatalogImportErrors.UserCannotApplyCatalogImport());
        }

        if (_dbContext.Entry(batch).State == EntityState.Detached)
        {
            _dbContext.CatalogImportBatches.Attach(batch);
        }

        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken)
            .ConfigureAwait(false);

        var prepareResult = await PrepareApplicationAsync(
            batch,
            appliedByUserId,
            cancellationToken)
            .ConfigureAwait(false);

        if (prepareResult.IsFailure)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);

            return Result.Failure<CatalogImportApplyExecutionResult, DomainError>(
                prepareResult.Error);
        }

        var feedbackFinalizationResult = await _recognitionFeedbackFinalizer.FinalizeForAppliedBatchAsync(
            batch.Id,
            appliedByUserId,
            appliedByUserType,
            cancellationToken).ConfigureAwait(false);

        if (feedbackFinalizationResult.IsFailure)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);

            return Result.Failure<CatalogImportApplyExecutionResult, DomainError>(
                feedbackFinalizationResult.Error);
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            LogApplyFailed(_logger, batch.Id, exception);

            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);

            return Result.Failure<CatalogImportApplyExecutionResult, DomainError>(
                CatalogImportErrors.ApplyConcurrencyConflict());
        }
        catch (DbUpdateException exception)
        {
            LogApplyFailed(_logger, batch.Id, exception);

            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);

            return Result.Failure<CatalogImportApplyExecutionResult, DomainError>(
                CatalogImportErrors.ApplyDatabaseFailure());
        }
        catch (PostgresException exception)
            when (string.Equals(
                exception.SqlState,
                PostgresErrorCodes.SerializationFailure,
                StringComparison.Ordinal))
        {
            LogApplyFailed(_logger, batch.Id, exception);

            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);

            return Result.Failure<CatalogImportApplyExecutionResult, DomainError>(
                CatalogImportErrors.ApplyConcurrencyConflict());
        }

        await TryAggregateRecognitionCandidatesAfterCommitAsync(batch.Id).ConfigureAwait(false);
        await TryPromoteRecognitionCandidatesAfterCommitAsync(batch.Id, appliedByUserId).ConfigureAwait(false);

        return Result.Success<CatalogImportApplyExecutionResult, DomainError>(
            prepareResult.Value);
    }

    private async Task TryAggregateRecognitionCandidatesAfterCommitAsync(Guid batchId)
    {
        try
        {
            var aggregationResult = await _recognitionCandidateAggregator.AggregateAsync(cancellationToken: CancellationToken.None).ConfigureAwait(false);

            if (aggregationResult.IsFailure)
            {
                LogCandidateAggregationRejected(
                    _logger,
                    batchId,
                    aggregationResult.Error.Code,
                    aggregationResult.Error.Message,
                    null);

                return;
            }

            var aggregation = aggregationResult.Value;

            LogCandidateAggregationCompleted(
                _logger,
                batchId,
                aggregation.ScannedFeedbackCount,
                aggregation.CreatedCandidateCount,
                aggregation.UpdatedCandidateCount,
                aggregation.AddedEvidenceCount,
                null);
        }
        catch (DbUpdateException exception)
        {
            LogCandidateAggregationFailed(_logger, batchId, exception);
        }
        catch (NpgsqlException exception)
        {
            LogCandidateAggregationFailed(_logger, batchId, exception);
        }
    }

    private async Task TryPromoteRecognitionCandidatesAfterCommitAsync(Guid batchId, Guid createdByUserId)
    {
        try
        {
            var promotionResult = await _recognitionCandidateSuggestionPromoter.PromoteEligibleAsync(
                createdByUserId,
                cancellationToken: CancellationToken.None).ConfigureAwait(false);

            if (promotionResult.IsFailure)
            {
                LogSuggestionPromotionRejected(
                    _logger,
                    batchId,
                    promotionResult.Error.Code,
                    promotionResult.Error.Message,
                    null);

                return;
            }

            var promotion = promotionResult.Value;

            LogSuggestionPromotionCompleted(
                _logger,
                batchId,
                promotion.ScannedCandidateCount,
                promotion.EligibleCandidateCount,
                promotion.CreatedSuggestionCount,
                promotion.AttachedExistingSuggestionCount,
                null);
        }
        catch (DbUpdateException exception)
        {
            LogSuggestionPromotionFailed(_logger, batchId, exception);
        }
        catch (NpgsqlException exception)
        {
            LogSuggestionPromotionFailed(_logger, batchId, exception);
        }
    }

    private async Task<Result<CatalogImportApplyExecutionResult, DomainError>> PrepareApplicationAsync(
        CatalogImportBatch batch,
        Guid appliedByUserId,
        CancellationToken cancellationToken)
    {
        var startApplyingResult = batch.StartApplying(appliedByUserId);

        if (startApplyingResult.IsFailure)
        {
            return Result.Failure<CatalogImportApplyExecutionResult, DomainError>(
                startApplyingResult.Error);
        }

        var rows = await _dbContext.CatalogImportRows
            .AsNoTracking()
            .Where(row => row.BatchId == batch.Id)
            .OrderBy(row => row.RowNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (rows.Count == 0
            || rows.Count != batch.RowsCount
            || rows.Count != batch.ValidRowsCount
            || rows.Any(row => row.Status != CatalogImportRowStatus.Valid))
        {
            return Result.Failure<CatalogImportApplyExecutionResult, DomainError>(
                CatalogImportErrors.InvalidRowsStatistics());
        }

        var preparedRowsResult = PrepareRows(rows);

        if (preparedRowsResult.IsFailure)
        {
            return Result.Failure<CatalogImportApplyExecutionResult, DomainError>(
                preparedRowsResult.Error);
        }

        var preparedRows = preparedRowsResult.Value;

        var existingArticles = await _dbContext.Products
            .AsNoTracking()
            .Select(product => product.Article.Value)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var existingArticlesSet = existingArticles.ToHashSet(
            StringComparer.OrdinalIgnoreCase);

        foreach (var preparedRow in preparedRows)
        {
            var article = preparedRow.Data.Article!;

            if (existingArticlesSet.Contains(article))
            {
                return Result.Failure<CatalogImportApplyExecutionResult, DomainError>(
                    CatalogImportErrors.ProductArticleAlreadyExists(
                        article,
                        preparedRow.RowNumber));
            }
        }

        if (batch.ProductTypeId is not Guid productTypeId)
        {
            return Result.Failure<CatalogImportApplyExecutionResult, DomainError>(
                CatalogImportErrors.ProductTypeIsRequired());
        }

        var productType = await _dbContext.ProductTypes
            .Include(type => type.Characteristics)
            .FirstOrDefaultAsync(
                type => type.Id == productTypeId,
                cancellationToken)
            .ConfigureAwait(false);

        if (productType is null)
        {
            return Result.Failure<CatalogImportApplyExecutionResult, DomainError>(
                CatalogImportErrors.ProductTypeNotFound(productTypeId));
        }

        var allowedDefinitionIds = productType.Characteristics
            .Select(characteristic => characteristic.CharacteristicDefinitionId)
            .Distinct()
            .ToArray();

        var definitions = allowedDefinitionIds.Length == 0
            ? []
            : await _dbContext.CharacteristicDefinitions
                .Where(definition => allowedDefinitionIds.Contains(definition.Id))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        var definitionsById = definitions.ToDictionary(
            definition => definition.Id);

        var missingDefinitionId = allowedDefinitionIds
            .Where(definitionId => !definitionsById.ContainsKey(definitionId))
            .Select(definitionId => (Guid?)definitionId)
            .FirstOrDefault();

        if (missingDefinitionId.HasValue)
        {
            return Result.Failure<CatalogImportApplyExecutionResult, DomainError>(
                CatalogImportErrors.CharacteristicDefinitionNotFound(
                    missingDefinitionId.Value));
        }

        var manufacturerIds = preparedRows
            .Select(row => row.Data.ManufacturerId!.Value)
            .Distinct()
            .ToArray();

        var manufacturers = await _dbContext.Manufacturers
            .Where(manufacturer => manufacturerIds.Contains(manufacturer.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var manufacturersById = manufacturers.ToDictionary(
            manufacturer => manufacturer.Id);

        var missingManufacturerId = manufacturerIds
            .Where(manufacturerId => !manufacturersById.ContainsKey(manufacturerId))
            .Select(manufacturerId => (Guid?)manufacturerId)
            .FirstOrDefault();

        if (missingManufacturerId.HasValue)
        {
            return Result.Failure<CatalogImportApplyExecutionResult, DomainError>(
                CatalogImportErrors.ManufacturerNotFound(
                    missingManufacturerId.Value));
        }

        var createdProductsCount = 0;

        foreach (var preparedRow in preparedRows)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var data = preparedRow.Data;
            var manufacturerId = data.ManufacturerId!.Value;

            var moneyResult = Money.Create(data.Price ?? 0m);

            if (moneyResult.IsFailure)
            {
                return Result.Failure<CatalogImportApplyExecutionResult, DomainError>(
                    moneyResult.Error);
            }

            var stockResult = StockQuantity.Create(data.StockQuantity ?? 0);

            if (stockResult.IsFailure)
            {
                return Result.Failure<CatalogImportApplyExecutionResult, DomainError>(
                    stockResult.Error);
            }

            var productResult = Product.Create(
                data.Article!,
                data.Name!,
                productType.Id,
                manufacturerId,
                moneyResult.Value,
                stockResult.Value);

            if (productResult.IsFailure)
            {
                return Result.Failure<CatalogImportApplyExecutionResult, DomainError>(
                    productResult.Error);
            }

            var product = productResult.Value;

            foreach (var characteristic in data.Characteristics)
            {
                if (!Guid.TryParse(
                    characteristic.Key,
                    out var characteristicDefinitionId))
                {
                    return Result.Failure<CatalogImportApplyExecutionResult, DomainError>(
                        CatalogImportErrors.InvalidNormalizedRow(
                            preparedRow.RowNumber));
                }

                if (!productType.AllowsCharacteristic(characteristicDefinitionId))
                {
                    return Result.Failure<CatalogImportApplyExecutionResult, DomainError>(
                        CatalogImportErrors.CharacteristicNotAllowed(
                            characteristicDefinitionId,
                            productType.Id));
                }

                if (!definitionsById.TryGetValue(
                    characteristicDefinitionId,
                    out var definition))
                {
                    return Result.Failure<CatalogImportApplyExecutionResult, DomainError>(
                        CatalogImportErrors.CharacteristicDefinitionNotFound(
                            characteristicDefinitionId));
                }

                var valueResult = CreateCharacteristicValue(
                    preparedRow.RowNumber,
                    definition,
                    characteristic.Value);

                if (valueResult.IsFailure)
                {
                    return Result.Failure<CatalogImportApplyExecutionResult, DomainError>(
                        valueResult.Error);
                }

                var setResult = product.SetCharacteristic(
                    productType,
                    definition,
                    valueResult.Value);

                if (setResult.IsFailure)
                {
                    return Result.Failure<CatalogImportApplyExecutionResult, DomainError>(
                        setResult.Error);
                }
            }

            var requiredValidationResult = product
                .ValidateRequiredCharacteristics(productType);

            if (requiredValidationResult.IsFailure)
            {
                return Result.Failure<CatalogImportApplyExecutionResult, DomainError>(
                    requiredValidationResult.Error);
            }

            _dbContext.Products.Add(product);

            var manufacturer = manufacturersById[manufacturerId];

            var afterSnapshot = ProductAuditSnapshotFactory.Create(
                product,
                productType,
                manufacturer,
                definitionsById);

            var afterJson = ProductAuditSnapshotSerializer.Serialize(
                afterSnapshot);

            var auditEntryResult = ProductAuditEntry.Create(
                product.Id,
                appliedByUserId,
                ProductAuditOperation.ImportApplied,
                ProductAuditSource.ImportBatch,
                batch.Id,
                beforeJson: null,
                afterJson);

            if (auditEntryResult.IsFailure)
            {
                return Result.Failure<CatalogImportApplyExecutionResult, DomainError>(
                    auditEntryResult.Error);
            }

            _dbContext.ProductAuditEntries.Add(
                auditEntryResult.Value);

            createdProductsCount++;
        }

        var completeApplyingResult = batch.CompleteApplying();

        if (completeApplyingResult.IsFailure)
        {
            return Result.Failure<CatalogImportApplyExecutionResult, DomainError>(
                completeApplyingResult.Error);
        }

        return Result.Success<CatalogImportApplyExecutionResult, DomainError>(
            new CatalogImportApplyExecutionResult(
                createdProductsCount));
    }

    private static Result<List<PreparedImportRow>, DomainError> PrepareRows(
        List<CatalogImportRow> rows)
    {
        var preparedRows = new List<PreparedImportRow>(rows.Count);

        var firstRowsByArticle = new Dictionary<string, int>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            CatalogImportNormalizedRowData? data;

            try
            {
                data = JsonSerializer.Deserialize<CatalogImportNormalizedRowData>(
                    row.NormalizedDataJson,
                    JsonOptions);
            }
            catch (JsonException)
            {
                return Result.Failure<List<PreparedImportRow>, DomainError>(
                    CatalogImportErrors.InvalidNormalizedRow(
                        row.RowNumber));
            }
            catch (NotSupportedException)
            {
                return Result.Failure<List<PreparedImportRow>, DomainError>(
                    CatalogImportErrors.InvalidNormalizedRow(
                        row.RowNumber));
            }

            if (data is null
                || string.IsNullOrWhiteSpace(data.Name)
                || string.IsNullOrWhiteSpace(data.Article)
                || data.ManufacturerId is null
                || data.ManufacturerId == Guid.Empty
                || data.Characteristics is null)
            {
                return Result.Failure<List<PreparedImportRow>, DomainError>(
                    CatalogImportErrors.InvalidNormalizedRow(
                        row.RowNumber));
            }

            var normalizedArticle = data.Article.Trim();

            if (firstRowsByArticle.TryGetValue(
                normalizedArticle,
                out var firstRowNumber))
            {
                return Result.Failure<List<PreparedImportRow>, DomainError>(
                    CatalogImportErrors.DuplicateArticleInBatch(
                        normalizedArticle,
                        firstRowNumber,
                        row.RowNumber));
            }

            firstRowsByArticle.Add(
                normalizedArticle,
                row.RowNumber);

            preparedRows.Add(
                new PreparedImportRow(
                    row.RowNumber,
                    data));
        }

        return Result.Success<List<PreparedImportRow>, DomainError>(
            preparedRows);
    }

    private static Result<CharacteristicValue, DomainError> CreateCharacteristicValue(
        int rowNumber,
        CharacteristicDefinition definition,
        string rawValue)
    {
        switch (definition.DataType)
        {
            case CharacteristicDataType.Text:
                return CharacteristicValue.CreateText(rawValue);

            case CharacteristicDataType.Number:
                if (decimal.TryParse(
                    rawValue,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var number))
                {
                    return CharacteristicValue.CreateNumber(number);
                }

                break;

            case CharacteristicDataType.Boolean:
                if (CatalogCharacteristicBooleanValueNormalizer.TryNormalize(
                        definition.Code,
                        rawValue,
                        out var boolean))
                {
                    return CharacteristicValue.CreateBoolean(boolean);
                }

                break;
        }

        return Result.Failure<CharacteristicValue, DomainError>(
            CatalogImportErrors.InvalidCharacteristicValue(
                rowNumber,
                definition.Id));
    }

    private sealed record PreparedImportRow(
        int RowNumber,
        CatalogImportNormalizedRowData Data);
}