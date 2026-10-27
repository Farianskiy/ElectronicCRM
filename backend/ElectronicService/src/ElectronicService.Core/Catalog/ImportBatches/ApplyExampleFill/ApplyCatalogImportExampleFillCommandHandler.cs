using System.Text.Json;
using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.ImportBatches.Abstractions;
using ElectronicService.Core.Catalog.ImportBatches.Analysis;
using ElectronicService.Core.Catalog.ImportBatches.BulkUpdateCatalogImportRows;
using ElectronicService.Core.Catalog.ImportBatches.PreviewExampleFill;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.ImportBatches.ApplyExampleFill;

public sealed class ApplyCatalogImportExampleFillCommandHandler
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

    private readonly PreviewCatalogImportExampleFillQueryHandler _previewHandler;
    private readonly BulkUpdateCatalogImportRowsCommandHandler _bulkUpdateHandler;
    private readonly ICatalogImportBatchRepository _importBatchRepository;

    public ApplyCatalogImportExampleFillCommandHandler(
        PreviewCatalogImportExampleFillQueryHandler previewHandler,
        BulkUpdateCatalogImportRowsCommandHandler bulkUpdateHandler,
        ICatalogImportBatchRepository importBatchRepository)
    {
        ArgumentNullException.ThrowIfNull(previewHandler);
        ArgumentNullException.ThrowIfNull(bulkUpdateHandler);
        ArgumentNullException.ThrowIfNull(importBatchRepository);

        _previewHandler = previewHandler;
        _bulkUpdateHandler = bulkUpdateHandler;
        _importBatchRepository = importBatchRepository;
    }

    public async Task<Result<BulkUpdateCatalogImportRowsResult, DomainError>> Handle(
        ApplyCatalogImportExampleFillCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var targetRowIds = command.TargetRowIds?
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray()
            ?? [];

        if (targetRowIds.Length == 0)
        {
            return Result.Failure<BulkUpdateCatalogImportRowsResult, DomainError>(
                CatalogImportErrors.ExampleFillApplyRequiresTargetRows());
        }

        var previewResult = await _previewHandler
            .Handle(
                new PreviewCatalogImportExampleFillQuery(
                    command.BatchId,
                    command.CurrentUserId,
                    command.ExpectedVersion,
                    command.ExampleRowIds,
                    targetRowIds),
                cancellationToken)
            .ConfigureAwait(false);

        if (previewResult.IsFailure)
        {
            return Result.Failure<BulkUpdateCatalogImportRowsResult, DomainError>(
                previewResult.Error);
        }

        var rows = await _importBatchRepository
            .GetRowsByIdsAsync(command.BatchId, targetRowIds, cancellationToken)
            .ConfigureAwait(false);
        var previewRowsById = previewResult.Value.Rows.ToDictionary(row => row.RowId);
        var commandRows = new List<BulkUpdateCatalogImportRowItem>(rows.Count);

        foreach (var row in rows.OrderBy(item => item.RowNumber))
        {
            if (!previewRowsById.TryGetValue(row.Id, out var previewRow)
                || previewRow.Proposals.Count == 0
                || previewRow.Conflicts.Count > 0)
            {
                return Result.Failure<BulkUpdateCatalogImportRowsResult, DomainError>(
                    CatalogImportErrors.ExampleFillApplyNotReady(row.RowNumber));
            }

            var data = Deserialize(row.NormalizedDataJson);
            if (data is null)
            {
                return Result.Failure<BulkUpdateCatalogImportRowsResult, DomainError>(
                    CatalogImportErrors.InvalidImportJson(
                        nameof(CatalogImportRow.NormalizedDataJson)));
            }

            var characteristics = data.Characteristics.ToDictionary(
                item => item.Key,
                item => item.Value,
                StringComparer.Ordinal);
            var origins = (data.CharacteristicOrigins
                    ?? new Dictionary<string, CatalogImportCharacteristicValueOrigin>(
                        StringComparer.Ordinal))
                .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);

            foreach (var proposal in previewRow.Proposals)
            {
                var characteristicId = proposal.CharacteristicDefinitionId.ToString();
                characteristics[characteristicId] = proposal.ProposedValue;
                origins[characteristicId] =
                    CatalogImportCharacteristicValueOrigin.FromTrainingExamples(
                        proposal.MatchedRawValue,
                        proposal.Confidence,
                        proposal.SourceExampleIds,
                        proposal.SourceRowIds);
            }

            commandRows.Add(new BulkUpdateCatalogImportRowItem(
                row.Id,
                data.Name,
                data.Article,
                data.ProductTypeId,
                data.ManufacturerId,
                data.Price,
                data.StockQuantity,
                characteristics,
                origins,
                PreserveProductTypeResolution: true));
        }

        return await _bulkUpdateHandler
            .Handle(
                new BulkUpdateCatalogImportRowsCommand(
                    command.BatchId,
                    command.CurrentUserId,
                    command.ExpectedVersion,
                    commandRows,
                    ConfirmRecognitionSuggestions: false),
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static CatalogImportNormalizedRowData? Deserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<CatalogImportNormalizedRowData>(json, JsonOptions);
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
