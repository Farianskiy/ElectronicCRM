using ElectronicService.Contracts.Catalog.ImportBatches;
using ElectronicService.Core.Catalog.ImportBatches.Analysis;
using ElectronicService.Core.Catalog.ImportBatches.ApplyExampleFill;
using ElectronicService.Core.Catalog.ImportBatches.BulkUpdateCatalogImportRows;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Web.Auth;
using ElectronicService.Web.Controllers.Catalog.ImportBatches.Common;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.ImportBatches.ApplyExampleFill;

[ApiController]
[PermissionAuthorize(UserPermissionCode.CatalogImportsCreate)]
[Route("api/catalog/import-batches")]
public sealed class ApplyCatalogImportExampleFillController : ControllerBase
{
    private const string ProblemTitle = "Не удалось заполнить строки по примерам.";

    [HttpPost("{batchId:guid}/example-fill/apply")]
    [ProducesResponseType(typeof(BulkUpdateCatalogImportRowsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BulkUpdateCatalogImportRowsResponse>> Apply(
        Guid batchId,
        [FromBody] ApplyCatalogImportExampleFillRequest request,
        [FromServices] ApplyCatalogImportExampleFillCommandHandler handler,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var currentUserId))
        {
            return this.ToCurrentUserProblem();
        }

        var result = await handler
            .Handle(
                new ApplyCatalogImportExampleFillCommand(
                    batchId,
                    currentUserId,
                    request.ExpectedVersion,
                    request.ExampleRowIds,
                    request.TargetRowIds),
                cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return this.ToCatalogImportProblem(result.Error, ProblemTitle);
        }

        return Ok(MapResponse(result.Value));
    }

    private static BulkUpdateCatalogImportRowsResponse MapResponse(
        BulkUpdateCatalogImportRowsResult value) =>
        new(
            value.Rows.Select(row => new BulkUpdatedCatalogImportRowResponse(
                row.RowId,
                row.RowNumber,
                row.RowStatus.ToString(),
                new CatalogImportNormalizedRowResponse(
                    row.Data.Name,
                    row.Data.Article,
                    row.Data.Manufacturer,
                    row.Data.ManufacturerId,
                    row.Data.Price,
                    row.Data.StockQuantity,
                    row.Data.Characteristics,
                    MapOrigins(row.Data.CharacteristicOrigins),
                    row.Data.ProductTypeId,
                    row.Data.ProductTypeResolutionSource,
                    row.Data.ProductTypeResolutionConfidence),
                row.Issues.Select(issue => new CatalogImportRowIssueResponse(
                    issue.Code,
                    issue.Message,
                    issue.Field,
                    issue.SourceColumnNumber)).ToArray(),
                row.Warnings.Select(warning => new CatalogImportRowIssueResponse(
                    warning.Code,
                    warning.Message,
                    warning.Field,
                    warning.SourceColumnNumber)).ToArray())).ToArray(),
            value.BatchStatus.ToString(),
            value.RowsCount,
            value.ValidRowsCount,
            value.ErrorRowsCount,
            value.Version);

    private static Dictionary<string, CatalogImportCharacteristicOriginResponse> MapOrigins(
        IReadOnlyDictionary<string, CatalogImportCharacteristicValueOrigin>? origins) =>
        (origins
            ?? new Dictionary<string, CatalogImportCharacteristicValueOrigin>(
                StringComparer.Ordinal))
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
