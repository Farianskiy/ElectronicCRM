using ElectronicService.Contracts.Catalog.ImportBatches;
using ElectronicService.Core.Catalog.ImportBatches.GetManufacturerGroups;
using ElectronicService.Web.Auth;
using ElectronicService.Web.Controllers.Catalog.ImportBatches.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog.ImportBatches.GetManufacturerGroups;

[ApiController]
[Authorize(Roles = "Regular,Manager,Technical")]
[Route("api/catalog/import-batches")]
public sealed class GetCatalogImportManufacturerGroupsController : ControllerBase
{
    [HttpGet("{batchId:guid}/rows/manufacturer-groups")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(GetCatalogImportManufacturerGroupsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GetCatalogImportManufacturerGroupsResponse>> Get(
        Guid batchId,
        [FromQuery] uint? expectedVersion,
        [FromServices] GetCatalogImportManufacturerGroupsQueryHandler handler,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var currentUserId))
        {
            return this.ToCurrentUserProblem();
        }

        var query = new GetCatalogImportManufacturerGroupsQuery(
            batchId,
            currentUserId,
            expectedVersion);

        var result = await handler
            .Handle(
                query,
                cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return this.ToCatalogImportProblem(
                result.Error,
                "Не удалось получить группы производителей.");
        }

        return Ok(
            new GetCatalogImportManufacturerGroupsResponse(
                result.Value.BatchId,
                result.Value.BatchVersion,
                result.Value.Items
                    .Select(item =>
                        new CatalogImportManufacturerGroupResponse(
                            item.GroupKey,
                            item.SourceValue,
                            item.ResolutionSource,
                            item.ResolvedManufacturerName,
                            item.ExactNameRowsCount,
                            item.ApprovedAliasRowsCount,
                            item.IgnoredNoiseRowsCount,
                            item.UnresolvedRowsCount,
                            item.ManualRowsCount,
                            item.RowsCount))
                    .ToArray()));
    }
}