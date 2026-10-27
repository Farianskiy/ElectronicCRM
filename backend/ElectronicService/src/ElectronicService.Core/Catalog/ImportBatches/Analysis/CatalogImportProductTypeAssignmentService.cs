using System.Text.Json;
using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Products.Analogies;
using ElectronicService.Core.Catalog.ProductTypes.Suggestions;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Domain.Catalog.ProductTypes;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.ImportBatches.Analysis;

public sealed class CatalogImportProductTypeAssignmentService
    : ICatalogImportProductTypeAssignmentService
{
    private const decimal MinimumAutomaticConfidence = 0.9800m;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

    private readonly ICatalogProductTypeSuggestionService
        _suggestionService;
    private readonly ICatalogProductAnalogyReader? _analogyReader;

    public CatalogImportProductTypeAssignmentService(
        ICatalogProductTypeSuggestionService suggestionService,
        ICatalogProductAnalogyReader? analogyReader = null)
    {
        ArgumentNullException.ThrowIfNull(suggestionService);

        _suggestionService = suggestionService;
        _analogyReader = analogyReader;
    }

    public async Task<
        Result<CatalogImportWorkbookAnalysis, DomainError>>
        AssignAsync(
            CatalogImportWorkbookAnalysis analysis,
            ProductType? batchProductType,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(analysis);

        if (analysis.MappingRequired)
        {
            return Result.Success<
                CatalogImportWorkbookAnalysis,
                DomainError>(analysis);
        }

        CatalogProductTypeSuggestionIndex? suggestionIndex = null;
        var analogySnapshots = new Dictionary<
            Guid,
            CatalogProductAnalogyCandidateSnapshot>();

        if (batchProductType is null)
        {
            suggestionIndex = await _suggestionService
                .LoadIndexAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        foreach (var row in analysis.Rows
                     .OrderBy(row => row.RowNumber))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var data = DeserializeData(row.NormalizedDataJson);
            var issues = DeserializeIssues(row.IssuesJson);
            var warnings = DeserializeIssues(row.WarningsJson);

            if (data is null
                || issues is null
                || warnings is null)
            {
                return Result.Failure<
                    CatalogImportWorkbookAnalysis,
                    DomainError>(
                        CatalogImportErrors.InvalidNormalizedRow(
                            row.RowNumber));
            }

            if (string.IsNullOrWhiteSpace(data.Name))
            {
                continue;
            }

            if (string.Equals(data.ProductTypeResolutionSource, "Manual", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            Guid productTypeId;
            string resolutionSource;
            decimal resolutionConfidence;
            string? productTypeName = null;
            var automaticallyResolved = false;

            if (batchProductType is not null)
            {
                productTypeId = batchProductType.Id;
                productTypeName = batchProductType.Name;
                resolutionSource = "BatchDefault";
                resolutionConfidence = 1.0000m;
            }
            else
            {
                var productTypeEvidence = string.IsNullOrWhiteSpace(
                    data.RequestedProductType)
                    ? data.Name
                    : data.RequestedProductType;
                var suggestion = suggestionIndex!.Suggest(
                    productTypeEvidence,
                    cancellationToken);

                var candidate = suggestion.SelectedCandidate;
                var analogyProposal = candidate is not null
                                      && suggestion.IsSuggested
                                      && candidate.Confidence >= MinimumAutomaticConfidence
                    ? null
                    : await TryResolveFromCatalogExamplesAsync(
                            data,
                            analogySnapshots,
                            cancellationToken)
                        .ConfigureAwait(false);

                if (analogyProposal is null
                    && (!suggestion.IsSuggested
                        || candidate is null
                        || candidate.Confidence
                            < MinimumAutomaticConfidence))
                {
                    var issueCode = "product_type.not_resolved";
                    var issueMessage =
                        "Тип товара не удалось определить по наименованию. Выберите тип вручную.";

                    if (suggestion.IsConflict)
                    {
                        issueCode = "product_type.conflict";
                        issueMessage =
                            "По наименованию найдено несколько возможных типов товара. Выберите тип вручную.";
                    }
                    else if (candidate is not null
                             && candidate.Confidence < MinimumAutomaticConfidence)
                    {
                        issueCode = "product_type.low_confidence";
                        issueMessage =
                            $"Тип товара '{candidate.ProductTypeName}' распознан с недостаточной уверенностью ({candidate.Confidence:P0}). Подтвердите тип вручную.";
                    }

                    var unresolvedIssues = issues
                        .Where(issue =>
                            !string.Equals(
                                issue.Code,
                                "product_type.required",
                                StringComparison.Ordinal))
                        .Append(
                            new CatalogImportRowIssue(
                                issueCode,
                                issueMessage,
                                "productTypeId",
                                null))
                        .Distinct()
                        .ToArray();

                    var unresolvedReplaceResult = row.ReplaceValidationResult(
                        CatalogImportRowStatus.Error,
                        row.NormalizedDataJson,
                        JsonSerializer.Serialize(unresolvedIssues, JsonOptions),
                        row.WarningsJson);

                    if (unresolvedReplaceResult.IsFailure)
                    {
                        return Result.Failure<
                            CatalogImportWorkbookAnalysis,
                            DomainError>(unresolvedReplaceResult.Error);
                    }

                    continue;
                }

                if (analogyProposal is not null)
                {
                    productTypeId = analogyProposal.ProductTypeId;
                    productTypeName = analogyProposal.ProductTypeName;
                    resolutionSource = "CatalogAnalogy";
                    resolutionConfidence = analogyProposal.SupportRatio;
                }
                else
                {
                    productTypeId = candidate!.ProductTypeId;
                    productTypeName = candidate.ProductTypeName;
                    resolutionSource = string.IsNullOrWhiteSpace(
                        data.RequestedProductType)
                        ? "Automatic"
                        : "ExcelProductType";
                    resolutionConfidence = candidate.Confidence;
                }

                automaticallyResolved = true;
            }

            var enrichedData = data with
            {
                ProductTypeId = productTypeId,
                ProductTypeResolutionSource =
                    resolutionSource,
                ProductTypeResolutionConfidence =
                    resolutionConfidence
            };

            var nextIssues = automaticallyResolved
                ? issues
                    .Where(issue =>
                        !string.Equals(
                            issue.Code,
                            "product_type.required",
                            StringComparison.Ordinal)
                        && !string.Equals(
                            issue.Code,
                            "characteristics.analysis_pending",
                            StringComparison.Ordinal))
                    .Append(
                        new CatalogImportRowIssue(
                            "characteristics.analysis_pending",
                            "Тип товара определён. Требуется анализ характеристик строки.",
                            "characteristics",
                            null))
                    .Distinct()
                    .ToArray()
                : issues;

            var resolvedFromCatalogExamples = string.Equals(
                resolutionSource,
                "CatalogAnalogy",
                StringComparison.Ordinal);
            string resolutionWarningCode;
            string resolutionWarningMessage;
            if (resolvedFromCatalogExamples)
            {
                resolutionWarningCode = "product_type.resolved_from_catalog_examples";
                resolutionWarningMessage =
                    $"Тип товара '{productTypeName}' определён по похожим товарам этого производителя.";
            }
            else if (string.Equals(
                         resolutionSource,
                         "ExcelProductType",
                         StringComparison.Ordinal))
            {
                resolutionWarningCode = "product_type.resolved_from_excel";
                resolutionWarningMessage =
                    $"Тип товара '{productTypeName}' определён из колонки «Тип товара» или назначения позиции.";
            }
            else
            {
                resolutionWarningCode = "product_type.resolved_from_name";
                resolutionWarningMessage =
                    $"Тип товара '{productTypeName}' определён из наименования.";
            }

            var nextWarnings = automaticallyResolved
                ? warnings
                    .Append(
                        new CatalogImportRowIssue(
                            resolutionWarningCode,
                            resolutionWarningMessage,
                            "productTypeId",
                            null))
                    .Distinct()
                    .ToArray()
                : warnings;

            var nextStatus = automaticallyResolved
                ? CatalogImportRowStatus.Error
                : row.Status;

            var replaceResult = row.ReplaceValidationResult(
                nextStatus,
                JsonSerializer.Serialize(
                    enrichedData,
                    JsonOptions),
                JsonSerializer.Serialize(
                    nextIssues,
                    JsonOptions),
                JsonSerializer.Serialize(
                    nextWarnings,
                    JsonOptions));

            if (replaceResult.IsFailure)
            {
                return Result.Failure<
                    CatalogImportWorkbookAnalysis,
                    DomainError>(
                        replaceResult.Error);
            }
        }

        var validRowsCount = analysis.Rows.Count(
            row => row.Status
                == CatalogImportRowStatus.Valid);

        var errorRowsCount = analysis.Rows.Count(
            row => row.Status
                == CatalogImportRowStatus.Error);

        var enrichedAnalysis = analysis with
        {
            ValidRowsCount = validRowsCount,
            ErrorRowsCount = errorRowsCount
        };

        return Result.Success<
            CatalogImportWorkbookAnalysis,
            DomainError>(enrichedAnalysis);
    }

    private async Task<CatalogProductAnalogyProductTypeProposal?>
        TryResolveFromCatalogExamplesAsync(
            CatalogImportNormalizedRowData data,
            Dictionary<Guid, CatalogProductAnalogyCandidateSnapshot> snapshots,
            CancellationToken cancellationToken)
    {
        if (_analogyReader is null
            || !data.ManufacturerId.HasValue
            || data.ManufacturerId.Value == Guid.Empty
            || string.IsNullOrWhiteSpace(data.Name))
        {
            return null;
        }

        var manufacturerId = data.ManufacturerId.Value;

        if (!snapshots.TryGetValue(manufacturerId, out var snapshot))
        {
            snapshot = await _analogyReader
                .ReadCandidateSnapshotAsync(
                    manufacturerId,
                    null,
                    CatalogProductAnalogySettings.SystemDefault
                        .MaximumCandidatePoolSize,
                    cancellationToken)
                .ConfigureAwait(false);

            snapshots[manufacturerId] = snapshot;
        }

        var result = CatalogProductAnalogyEvaluator.Evaluate(
            new CatalogProductAnalogyRequest(
                data.Name,
                data.Article,
                manufacturerId,
                null,
                new Dictionary<string, string>(StringComparer.Ordinal),
                MaximumResults: 100),
            snapshot.Candidates,
            CatalogProductAnalogySettings.SystemDefault);

        return result.ProductTypeProposals
            .Where(proposal => proposal.IsSuggested)
            .OrderByDescending(proposal => proposal.SupportRatio)
            .ThenByDescending(proposal => proposal.SupportingProductCount)
            .ThenBy(proposal => proposal.ProductTypeName, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private static CatalogImportNormalizedRowData?
        DeserializeData(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<
                CatalogImportNormalizedRowData>(
                    json,
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

    private static CatalogImportRowIssue[]?
        DeserializeIssues(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<
                CatalogImportRowIssue[]>(
                    json,
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
