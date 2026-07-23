using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog
    .ImportBatches.Abstractions;
using ElectronicService.Core.Catalog
    .ImportBatches.Analysis;
using ElectronicService.Core.Catalog.Products
    .Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog
    .Characteristics;
using ElectronicService.Domain.Catalog
    .ImportBatches;
using ElectronicService.Domain.Catalog
    .ProductTypes;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog
    .ImportBatches.AnalyzeCatalogImportBatch;

public sealed class
    AnalyzeCatalogImportBatchCommandHandler
{
    private readonly ICatalogImportBatchRepository
        _importBatchRepository;

    private readonly IUserRepository
        _userRepository;

    private readonly ICatalogProductMetadataRepository
        _metadataRepository;

    private readonly ICatalogImportWorkbookAnalyzer
        _workbookAnalyzer;

    public AnalyzeCatalogImportBatchCommandHandler(
        ICatalogImportBatchRepository
            importBatchRepository,
        IUserRepository userRepository,
        ICatalogProductMetadataRepository
            metadataRepository,
        ICatalogImportWorkbookAnalyzer
            workbookAnalyzer)
    {
        _importBatchRepository =
            importBatchRepository;

        _userRepository =
            userRepository;

        _metadataRepository =
            metadataRepository;

        _workbookAnalyzer =
            workbookAnalyzer;
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
         * Manager может изменять только
         * собственные batch.
         *
         * Technical может проверять batch
         * любого Manager.
         */
        if (currentUser.IsManager
            && batch.CreatedByUserId
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

        var analysisResult =
            _workbookAnalyzer.Analyze(
                batch.Id,
                batch.File.Content,
                productType,
                definitions,
                cancellationToken);

        if (analysisResult.IsFailure)
        {
            return Result.Failure<
                AnalyzeCatalogImportBatchResult,
                DomainError>(
                    analysisResult.Error);
        }

        var analysis =
            analysisResult.Value;

        var registerResult =
            batch.RegisterAnalysisResult(
                analysis.Rows.Count,
                analysis.ValidRowsCount,
                analysis.ErrorRowsCount,
                analysis.MappingRequired);

        if (registerResult.IsFailure)
        {
            return Result.Failure<
                AnalyzeCatalogImportBatchResult,
                DomainError>(
                    registerResult.Error);
        }

        await _importBatchRepository
            .ReplaceAnalysisAsync(
                batch,
                analysis.Columns,
                analysis.Rows,
                cancellationToken)
            .ConfigureAwait(false);

        return Result.Success<
            AnalyzeCatalogImportBatchResult,
            DomainError>(
                new AnalyzeCatalogImportBatchResult(
                    batch.Id,
                    batch.Status,
                    batch.ProductTypeId,
                    analysis.Columns.Count,
                    analysis.Columns.Count(column =>
                        !column.IsMapped),
                    analysis.Columns.Count(column =>
                        !column.IsConfirmed),
                    analysis.Rows.Count,
                    analysis.ValidRowsCount,
                    analysis.ErrorRowsCount));
    }
}