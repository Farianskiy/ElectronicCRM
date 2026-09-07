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
using System.Text.Json;

namespace ElectronicService.Core.Catalog
    .ImportBatches.AnalyzeCatalogImportBatch;

public sealed class AnalyzeCatalogImportBatchCommandHandler
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

    private readonly ICatalogImportBatchRepository _importBatchRepository;

    private readonly IUserRepository _userRepository;

    private readonly ICatalogProductMetadataRepository _metadataRepository;

    private readonly ICatalogImportWorkbookAnalyzer _workbookAnalyzer;
    private readonly ICatalogImportProductTypeAssignmentService _productTypeAssignmentService;

    private readonly ICatalogImportManufacturerRecognitionShadowService _manufacturerRecognitionShadowService;
    private readonly ICatalogImportProductTypeSuggestionShadowService _productTypeSuggestionShadowService;
    private readonly ICatalogImportRecognitionShadowService _recognitionShadowService;
    private readonly ICatalogImportProductNameExplanationService _productNameExplanationService;
    private readonly ICatalogImportRecognitionEnrichmentService _recognitionEnrichmentService;
    private readonly ICatalogImportRecognitionFeedbackCollector _recognitionFeedbackCollector;
    private readonly IManufacturerResolver _manufacturerResolver;

    public AnalyzeCatalogImportBatchCommandHandler(
        ICatalogImportBatchRepository importBatchRepository,
        IUserRepository userRepository,
        ICatalogProductMetadataRepository metadataRepository,
        ICatalogImportWorkbookAnalyzer workbookAnalyzer,
        ICatalogImportProductTypeAssignmentService productTypeAssignmentService,
        ICatalogImportManufacturerRecognitionShadowService manufacturerRecognitionShadowService,
        ICatalogImportProductTypeSuggestionShadowService productTypeSuggestionShadowService,
        ICatalogImportRecognitionShadowService recognitionShadowService,
        ICatalogImportProductNameExplanationService productNameExplanationService,
        ICatalogImportRecognitionEnrichmentService recognitionEnrichmentService,
        ICatalogImportRecognitionFeedbackCollector recognitionFeedbackCollector,
        IManufacturerResolver manufacturerResolver)
    {
        ArgumentNullException.ThrowIfNull(importBatchRepository);
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(metadataRepository);
        ArgumentNullException.ThrowIfNull(workbookAnalyzer);
        ArgumentNullException.ThrowIfNull(productTypeAssignmentService);
        ArgumentNullException.ThrowIfNull(manufacturerRecognitionShadowService);
        ArgumentNullException.ThrowIfNull(productTypeSuggestionShadowService);
        ArgumentNullException.ThrowIfNull(recognitionShadowService);
        ArgumentNullException.ThrowIfNull(productNameExplanationService);
        ArgumentNullException.ThrowIfNull(recognitionEnrichmentService);
        ArgumentNullException.ThrowIfNull(recognitionFeedbackCollector);
        ArgumentNullException.ThrowIfNull(manufacturerResolver);

        _importBatchRepository = importBatchRepository;
        _userRepository = userRepository;
        _metadataRepository = metadataRepository;
        _workbookAnalyzer = workbookAnalyzer;
        _productTypeAssignmentService = productTypeAssignmentService;
        _manufacturerRecognitionShadowService = manufacturerRecognitionShadowService;
        _productTypeSuggestionShadowService = productTypeSuggestionShadowService;
        _recognitionShadowService = recognitionShadowService;
        _productNameExplanationService = productNameExplanationService;
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

        var productTypeAssignmentResult =
            await _productTypeAssignmentService
                .AssignAsync(
                    analysis,
                    productType,
                    cancellationToken)
                .ConfigureAwait(false);

        if (productTypeAssignmentResult.IsFailure)
        {
            return Result.Failure<
                AnalyzeCatalogImportBatchResult,
                DomainError>(
                    productTypeAssignmentResult.Error);
        }

        analysis = productTypeAssignmentResult.Value;

        var manufacturerRecognitionShadow =
                    _manufacturerRecognitionShadowService.Analyze(
                analysis,
                manufacturerResolutionIndex,
                cancellationToken);

        var productTypeSuggestionShadow =
            await _productTypeSuggestionShadowService
                .AnalyzeAsync(
                    analysis,
                    productType,
                    cancellationToken)
                .ConfigureAwait(false);

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
        else if (productType is null && !analysis.MappingRequired)
        {
            var enrichmentResult = await EnrichRowsByProductTypeAsync(
                    analysis,
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

        var productNameExplanation =
            _productNameExplanationService.Analyze(
                analysis,
                manufacturerRecognitionShadow,
                productTypeSuggestionShadow,
                recognitionShadow,
                cancellationToken);

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
                manufacturerRecognitionShadow,
                productTypeSuggestionShadow,
                recognitionShadow,
                productNameExplanation,
                recognitionEnrichment));
    }

    private async Task<Result<CatalogImportRecognitionEnrichmentResult, DomainError>>
        EnrichRowsByProductTypeAsync(
            CatalogImportWorkbookAnalysis analysis,
            CancellationToken cancellationToken)
    {
        var typedRows = new List<(CatalogImportRow Row, Guid ProductTypeId)>();

        foreach (var row in analysis.Rows)
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
                return Result.Failure<CatalogImportRecognitionEnrichmentResult, DomainError>(
                    CatalogImportErrors.InvalidNormalizedRow(row.RowNumber));
            }
            catch (NotSupportedException)
            {
                return Result.Failure<CatalogImportRecognitionEnrichmentResult, DomainError>(
                    CatalogImportErrors.InvalidNormalizedRow(row.RowNumber));
            }

            if (data?.ProductTypeId is Guid productTypeId
                && productTypeId != Guid.Empty)
            {
                typedRows.Add((row, productTypeId));
            }
        }

        var summaries = new List<CatalogImportRecognitionEnrichmentSummary>();

        foreach (var group in typedRows.GroupBy(item => item.ProductTypeId))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var rowProductType = await _metadataRepository
                .GetProductTypeByIdAsync(group.Key, cancellationToken)
                .ConfigureAwait(false);

            if (rowProductType is null)
            {
                return Result.Failure<CatalogImportRecognitionEnrichmentResult, DomainError>(
                    CatalogImportErrors.ProductTypeNotFound(group.Key));
            }

            var definitionIds = rowProductType.Characteristics
                .Select(characteristic =>
                    characteristic.CharacteristicDefinitionId)
                .Distinct()
                .ToArray();

            var rowDefinitions = await _metadataRepository
                .GetCharacteristicDefinitionsByIdsAsync(
                    definitionIds,
                    cancellationToken)
                .ConfigureAwait(false);

            var rows = group.Select(item => item.Row).ToArray();
            var groupAnalysis = analysis with
            {
                Rows = rows,
                ValidRowsCount = rows.Count(row =>
                    row.Status == CatalogImportRowStatus.Valid),
                ErrorRowsCount = rows.Count(row =>
                    row.Status == CatalogImportRowStatus.Error)
            };

            var enrichmentResult = await _recognitionEnrichmentService
                .EnrichAsync(
                    groupAnalysis,
                    rowProductType,
                    rowDefinitions,
                    cancellationToken)
                .ConfigureAwait(false);

            if (enrichmentResult.IsFailure)
            {
                return Result.Failure<CatalogImportRecognitionEnrichmentResult, DomainError>(
                    enrichmentResult.Error);
            }

            summaries.Add(enrichmentResult.Value.Summary);
        }

        var effectiveAnalysis = analysis with
        {
            ValidRowsCount = analysis.Rows.Count(row =>
                row.Status == CatalogImportRowStatus.Valid),
            ErrorRowsCount = analysis.Rows.Count(row =>
                row.Status == CatalogImportRowStatus.Error)
        };

        return Result.Success<CatalogImportRecognitionEnrichmentResult, DomainError>(
            new CatalogImportRecognitionEnrichmentResult(
                effectiveAnalysis,
                MergeEnrichmentSummaries(summaries)));
    }

    private static CatalogImportRecognitionEnrichmentSummary MergeEnrichmentSummaries(
        List<CatalogImportRecognitionEnrichmentSummary> summaries)
    {
        if (summaries.Count == 0)
        {
            return CatalogImportRecognitionEnrichmentSummary.Empty;
        }

        var appliedValues = summaries
            .SelectMany(summary => summary.AppliedValues)
            .Take(CatalogImportRecognitionEnrichmentService.MaximumAppliedValueDetails)
            .ToArray();

        var totalAppliedValueDetails = summaries.Sum(summary =>
            summary.AppliedValues.Count);

        return new CatalogImportRecognitionEnrichmentSummary(
            summaries.Sum(summary => summary.RowsAnalyzedCount),
            summaries.Sum(summary => summary.FilledRowsCount),
            summaries.Sum(summary => summary.FilledValuesCount),
            summaries.Sum(summary => summary.BlockedByRecognitionConflictCount),
            summaries.Sum(summary => summary.BlockedByLowConfidenceCount),
            summaries.Sum(summary => summary.BlockedByUnsupportedSourceCount),
            summaries.Sum(summary => summary.BlockedByInvalidExcelValueCount),
            summaries.Sum(summary => summary.BlockedByInvalidRecognizedValueCount),
            summaries.Sum(summary => summary.FailedRecognitionRowsCount),
            summaries.Any(summary => summary.AppliedValuesDetailsTruncated)
                || totalAppliedValueDetails > appliedValues.Length,
            appliedValues);
    }
}
