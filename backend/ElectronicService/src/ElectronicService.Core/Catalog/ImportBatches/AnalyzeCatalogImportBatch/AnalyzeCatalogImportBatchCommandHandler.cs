using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.ImportBatches.Abstractions;
using ElectronicService.Core.Catalog.ImportBatches.Analysis;
using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.Characteristics;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Domain.Catalog.ProductTypes;
using ElectronicService.Domain.Common;
using ElectronicService.Core.Catalog.Manufacturers.Resolution;

namespace ElectronicService.Core.Catalog
    .ImportBatches.AnalyzeCatalogImportBatch;

public sealed class AnalyzeCatalogImportBatchCommandHandler
{
    private readonly ICatalogImportBatchRepository _importBatchRepository;

    private readonly IUserRepository _userRepository;

    private readonly ICatalogProductMetadataRepository _metadataRepository;

    private readonly ICatalogImportWorkbookAnalyzer _workbookAnalyzer;

    private readonly ICatalogImportRecognitionShadowService _recognitionShadowService;
    private readonly ICatalogImportRecognitionEnrichmentService _recognitionEnrichmentService;
    private readonly ICatalogImportRecognitionFeedbackCollector _recognitionFeedbackCollector;
    private readonly IManufacturerResolver _manufacturerResolver;

    public AnalyzeCatalogImportBatchCommandHandler(
        ICatalogImportBatchRepository importBatchRepository,
        IUserRepository userRepository,
        ICatalogProductMetadataRepository metadataRepository,
        ICatalogImportWorkbookAnalyzer workbookAnalyzer,
        ICatalogImportRecognitionShadowService recognitionShadowService,
        ICatalogImportRecognitionEnrichmentService recognitionEnrichmentService,
        ICatalogImportRecognitionFeedbackCollector recognitionFeedbackCollector,
        IManufacturerResolver manufacturerResolver)
    {
        ArgumentNullException.ThrowIfNull(importBatchRepository);
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(metadataRepository);
        ArgumentNullException.ThrowIfNull(workbookAnalyzer);
        ArgumentNullException.ThrowIfNull(recognitionShadowService);
        ArgumentNullException.ThrowIfNull(recognitionEnrichmentService);
        ArgumentNullException.ThrowIfNull(recognitionFeedbackCollector);
        ArgumentNullException.ThrowIfNull(manufacturerResolver);

        _importBatchRepository = importBatchRepository;
        _userRepository = userRepository;
        _metadataRepository = metadataRepository;
        _workbookAnalyzer = workbookAnalyzer;
        _recognitionShadowService = recognitionShadowService;
        _recognitionEnrichmentService = recognitionEnrichmentService;
        _recognitionFeedbackCollector = recognitionFeedbackCollector;
        _manufacturerResolver = manufacturerResolver;
    }

    public async Task<Result<
        AnalyzeCatalogImportBatchResult,
        DomainError>> Handle(
            AnalyzeCatalogImportBatchCommand command,
            CancellationToken cancellationToken =
                default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.BatchId == Guid.Empty)
        {
            return Result.Failure<
                AnalyzeCatalogImportBatchResult,
                DomainError>(
                    CatalogImportErrors
                        .BatchNotFound(
                            command.BatchId));
        }

        if (command.CurrentUserId
            == Guid.Empty)
        {
            return Result.Failure<
                AnalyzeCatalogImportBatchResult,
                DomainError>(
                    CatalogImportErrors
                        .CurrentUserNotFound());
        }

        var currentUser =
            await _userRepository
                .GetByIdAsync(
                    command.CurrentUserId,
                    cancellationToken)
                .ConfigureAwait(false);

        if (currentUser is null)
        {
            return Result.Failure<
                AnalyzeCatalogImportBatchResult,
                DomainError>(
                    CatalogImportErrors
                        .CurrentUserNotFound());
        }

        if (!currentUser
                .CanEditCatalogImport())
        {
            return Result.Failure<
                AnalyzeCatalogImportBatchResult,
                DomainError>(
                    CatalogImportErrors
                        .UserCannotAccessBatch());
        }

        var batch =
            await _importBatchRepository
                .GetByIdWithFileAsync(
                    command.BatchId,
                    cancellationToken)
                .ConfigureAwait(false);

        if (batch is null)
        {
            return Result.Failure<
                AnalyzeCatalogImportBatchResult,
                DomainError>(
                    CatalogImportErrors
                        .BatchNotFound(
                            command.BatchId));
        }

        /*
 * Анализ изменяет колонки, строки
 * и статус пакета.
 *
 * Поэтому анализировать пакет может
 * только пользователь, который его создал.
 *
 * Technical получает доступ к чужому
 * пакету только после отправки на проверку.
 */
        if (batch.CreatedByUserId
            != currentUser.Id)
        {
            return Result.Failure<
                AnalyzeCatalogImportBatchResult,
                DomainError>(
                    CatalogImportErrors
                        .UserCannotAccessBatch());
        }

        if (!batch.IsEditable)
        {
            return Result.Failure<
                AnalyzeCatalogImportBatchResult,
                DomainError>(
                    CatalogImportErrors
                        .BatchCannotBeAnalyzed(
                            batch.Status));
        }

        ProductType? productType = null;

        var effectiveProductTypeId =
            command.ProductTypeId
            ?? batch.ProductTypeId;

        if (effectiveProductTypeId.HasValue)
        {
            if (effectiveProductTypeId.Value
                == Guid.Empty)
            {
                return Result.Failure<
                    AnalyzeCatalogImportBatchResult,
                    DomainError>(
                        CatalogImportErrors
                            .ProductTypeNotFound(
                                effectiveProductTypeId
                                    .Value));
            }

            productType =
                await _metadataRepository
                    .GetProductTypeByIdAsync(
                        effectiveProductTypeId.Value,
                        cancellationToken)
                    .ConfigureAwait(false);

            if (productType is null)
            {
                return Result.Failure<
                    AnalyzeCatalogImportBatchResult,
                    DomainError>(
                        CatalogImportErrors
                            .ProductTypeNotFound(
                                effectiveProductTypeId
                                    .Value));
            }

            if (batch.ProductTypeId
                != productType.Id)
            {
                var assignResult =
                    batch.AssignProductType(
                        productType.Id);

                if (assignResult.IsFailure)
                {
                    return Result.Failure<
                        AnalyzeCatalogImportBatchResult,
                        DomainError>(
                            assignResult.Error);
                }
            }
        }

        IReadOnlyCollection<
            CharacteristicDefinition>
            definitions = [];

        if (productType is not null)
        {
            var definitionIds =
                productType.Characteristics
                    .Select(characteristic =>
                        characteristic
                            .CharacteristicDefinitionId)
                    .Distinct()
                    .ToArray();

            definitions =
                await _metadataRepository
                    .GetCharacteristicDefinitionsByIdsAsync(
                        definitionIds,
                        cancellationToken)
                    .ConfigureAwait(false);
        }

        var existingColumns =
            await _importBatchRepository
                .GetColumnsForAnalysisAsync(
                    batch.Id,
                    cancellationToken)
                .ConfigureAwait(false);

        var manufacturerResolutionIndex = await _manufacturerResolver
            .LoadIndexAsync(cancellationToken)
            .ConfigureAwait(false);

        var analysisResult = _workbookAnalyzer.Analyze(
            batch.Id,
            batch.File.Content,
            productType,
            definitions,
            manufacturerResolutionIndex,
            existingColumns,
            cancellationToken);

        if (analysisResult.IsFailure)
        {
            return Result.Failure<
                AnalyzeCatalogImportBatchResult,
                DomainError>(
                    analysisResult.Error);
        }

        var analysis = analysisResult.Value;

        CatalogImportRecognitionShadowResult? recognitionShadow = null;
        var recognitionEnrichment = CatalogImportRecognitionEnrichmentSummary.Empty;
        var effectiveAnalysis = analysis;

        if (productType is not null && definitions.Count > 0)
        {
            recognitionShadow = await _recognitionShadowService
                .AnalyzeAsync(
                    analysis,
                    productType,
                    definitions,
                    cancellationToken)
                .ConfigureAwait(false);

            var enrichmentResult = await _recognitionEnrichmentService
                .EnrichAsync(
                    analysis,
                    productType,
                    definitions,
                    cancellationToken)
                .ConfigureAwait(false);

            if (enrichmentResult.IsFailure)
            {
                return Result.Failure<AnalyzeCatalogImportBatchResult, DomainError>(
                    enrichmentResult.Error);
            }

            effectiveAnalysis = enrichmentResult.Value.Analysis;
            recognitionEnrichment = enrichmentResult.Value.Summary;
        }

        var registerResult = batch.RegisterAnalysisResult(
                effectiveAnalysis.Rows.Count,
                effectiveAnalysis.ValidRowsCount,
                effectiveAnalysis.ErrorRowsCount,
                effectiveAnalysis.MappingRequired);

        if (registerResult.IsFailure)
        {
            return Result.Failure<
                AnalyzeCatalogImportBatchResult,
                DomainError>(
                    registerResult.Error);
        }

        var pendingFeedbackRemovalResult = await _recognitionFeedbackCollector
            .RemovePendingForBatchAsync(batch.Id, cancellationToken)
            .ConfigureAwait(false);

        if (pendingFeedbackRemovalResult.IsFailure)
        {
            return Result.Failure<
                AnalyzeCatalogImportBatchResult,
                DomainError>(
                    pendingFeedbackRemovalResult.Error);
        }

        await _importBatchRepository
            .ReplaceAnalysisAsync(
                batch,
                effectiveAnalysis.Columns,
                effectiveAnalysis.Rows,
                cancellationToken)
            .ConfigureAwait(false);

        return Result.Success<AnalyzeCatalogImportBatchResult, DomainError>(
            new AnalyzeCatalogImportBatchResult(
                batch.Id,
                batch.Status,
                batch.ProductTypeId,
                effectiveAnalysis.Columns.Count,
                effectiveAnalysis.Columns.Count(column => !column.IsMapped),
                effectiveAnalysis.Columns.Count(column => !column.IsConfirmed),
                effectiveAnalysis.Rows.Count,
                effectiveAnalysis.ValidRowsCount,
                effectiveAnalysis.ErrorRowsCount,
                effectiveAnalysis.ManufacturerResolutionSummary,
                recognitionShadow,
                recognitionEnrichment));
    }
}