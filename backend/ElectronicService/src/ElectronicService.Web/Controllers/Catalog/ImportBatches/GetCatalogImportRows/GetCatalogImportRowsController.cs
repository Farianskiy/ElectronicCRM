using ElectronicService.Contracts.Catalog.ImportBatches;
using ElectronicService.Core.Catalog.ImportBatches.Analysis;
using ElectronicService.Core.Catalog.ImportBatches.GetCatalogImportRows;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ElectronicService.Web.Controllers.Catalog.ImportBatches.Common;

namespace ElectronicService.Web.Controllers.Catalog.ImportBatches.GetCatalogImportRows;

[ApiController]
[PermissionAnyAuthorize(UserPermissionCode.CatalogImportsCreate, UserPermissionCode.CatalogImportsReview)]
[Route("api/catalog/import-batches")]
public sealed class
    GetCatalogImportRowsController
    : ControllerBase
{
    private const string ProblemTitle = "Не удалось выполнить операцию.";
    [HttpGet("{batchId:guid}/rows")]
    [ProducesResponseType(
    typeof(GetCatalogImportRowsResponse),
    StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetCatalogImportRowsResponse>> Get(
    Guid batchId,
    [FromServices] GetCatalogImportRowsQueryHandler handler,
    CancellationToken cancellationToken,
    [FromQuery] CatalogImportRowStatus? status = null,
    [FromQuery] CatalogImportRowProblemKind? problemKind = null,
    [FromQuery] string? search = null,
    [FromQuery] string? issueCode = null,
    [FromQuery] string? manufacturerGroupKey = null,
    [FromQuery] CatalogImportRowKnowledgeState? knowledgeState = null,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 50)
    {
        if (!User.TryGetUserId(out var currentUserId))
        {
            return this.ToCurrentUserProblem();
        }

        var query = new GetCatalogImportRowsQuery(
            batchId,
            currentUserId,
            status,
            search,
            issueCode,
            problemKind,
            manufacturerGroupKey,
            knowledgeState,
            page,
            pageSize);

        var result = await handler.Handle(
            query,
            cancellationToken);

        if (result.IsFailure)
        {
            return this.ToCatalogImportProblem(result.Error, ProblemTitle);
        }

        var items = result.Value.Items
            .Select(item =>
                new CatalogImportRowResponse(
                    item.RowId,
                    item.RowNumber,
                    item.Status.ToString(),
                    item.RawData,
                    new CatalogImportNormalizedRowResponse(
                        item.Data.Name,
                        item.Data.Article,
                        item.Data.Manufacturer,
                        item.Data.ManufacturerId,
                        item.Data.Price,
                        item.Data.StockQuantity,
                        item.Data.Characteristics,
                        MapOrigins(item.Data.CharacteristicOrigins),
                        item.Data.ProductTypeId,
                        item.Data.ProductTypeResolutionSource,
                        item.Data.ProductTypeResolutionConfidence),
                    item.Issues
                        .Select(issue =>
                            new CatalogImportRowIssueResponse(
                                issue.Code,
                                issue.Message,
                                issue.Field,
                                issue.SourceColumnNumber))
                        .ToArray(),
                    item.Warnings
                        .Select(warning =>
                            new CatalogImportRowIssueResponse(
                                warning.Code,
                                warning.Message,
                                warning.Field,
                                warning.SourceColumnNumber))
                        .ToArray()))
            .ToArray();

        var response = new GetCatalogImportRowsResponse(
            items,
            result.Value.Page,
            result.Value.PageSize,
            result.Value.TotalCount,
            result.Value.TotalPages);

        return Ok(response);
    }

    private static Dictionary<string, CatalogImportCharacteristicOriginResponse> MapOrigins(
        IReadOnlyDictionary<string, CatalogImportCharacteristicValueOrigin>? origins) =>
        (origins ?? new Dictionary<string, CatalogImportCharacteristicValueOrigin>(StringComparer.Ordinal))
        .ToDictionary(
            item => item.Key,
            item => new CatalogImportCharacteristicOriginResponse(
                item.Value.Source.ToString(),
                item.Value.Confidence,
                item.Value.RawValue,
                item.Value.AnalogyConfigurationId,
                item.Value.AnalogyConfigurationVersion,
                item.Value.AnalogySupportingProductIds ?? [],
                item.Value.AnalogyConfigurationHash,
                item.Value.AnalogyCandidateSnapshotId,
                item.Value.AnalogyCandidateSnapshotCreatedAtUtc,
                item.Value.AnalogyFamilyAnchor,
                item.Value.AnalogySupportingProductCount,
                item.Value.TrainingExampleIds ?? [],
                item.Value.TrainingSourceRowIds ?? []),
            StringComparer.Ordinal);
}
