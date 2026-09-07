using System.Text.Json;
using CSharpFunctionalExtensions;
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

    public CatalogImportProductTypeAssignmentService(
        ICatalogProductTypeSuggestionService suggestionService)
    {
        ArgumentNullException.ThrowIfNull(suggestionService);

        _suggestionService = suggestionService;
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
                var suggestion = suggestionIndex!.Suggest(
                    data.Name,
                    cancellationToken);

                var candidate = suggestion.SelectedCandidate;

                if (!suggestion.IsSuggested
                    || candidate is null
                    || candidate.Confidence
                        < MinimumAutomaticConfidence)
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

                productTypeId = candidate.ProductTypeId;
                productTypeName = candidate.ProductTypeName;
                resolutionSource = "Automatic";
                resolutionConfidence = candidate.Confidence;
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

            var nextWarnings = automaticallyResolved
                ? warnings
                    .Append(
                        new CatalogImportRowIssue(
                            "product_type.resolved_from_name",
                            $"Тип товара '{productTypeName}' определён из наименования.",
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
