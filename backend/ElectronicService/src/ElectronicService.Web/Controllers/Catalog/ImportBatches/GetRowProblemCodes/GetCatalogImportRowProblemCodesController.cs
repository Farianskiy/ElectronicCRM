using ElectronicService.Contracts.Catalog.ImportBatches;
using ElectronicService.Core.Catalog.ImportBatches
    .GetRowProblemCodes;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Web.Auth;
using ElectronicService.Web.Controllers.Catalog
    .ImportBatches.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicService.Web.Controllers.Catalog
    .ImportBatches.GetRowProblemCodes;

[ApiController]
[Authorize(Roles = "Regular,Manager,Technical")]
[Route("api/catalog/import-batches")]
public sealed class
    GetCatalogImportRowProblemCodesController
    : ControllerBase
{
    [HttpGet("{batchId:guid}/rows/problem-codes")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(GetCatalogImportRowProblemCodesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GetCatalogImportRowProblemCodesResponse>> Get(
            Guid batchId,
            [FromQuery] CatalogImportRowStatus? status,
            [FromQuery] string? search,
            [FromQuery] uint? expectedVersion,
            [FromServices] GetCatalogImportRowProblemCodesQueryHandler handler,
            CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(
            out var currentUserId))
        {
            return this.ToCurrentUserProblem();
        }

        var query =
            new GetCatalogImportRowProblemCodesQuery(
                batchId,
                currentUserId,
                status,
                search,
                expectedVersion);

        var result =
            await handler
                .Handle(
                    query,
                    cancellationToken)
                .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return this.ToCatalogImportProblem(
                result.Error,
                "Не удалось получить сводку проблем строк.");
        }

        return Ok(
            new GetCatalogImportRowProblemCodesResponse(
                result.Value.BatchId,
                result.Value.BatchVersion,
                result.Value.TotalRowsCount,
                result.Value.ErrorRowsCount,
                result.Value.WarningRowsCount,
                result.Value.Items
                    .Select(item =>
                        new CatalogImportRowProblemCodeResponse(
                            item.Code,
                            item.RowsCount,
                            item.ErrorRowsCount,
                            item.WarningRowsCount))
                    .ToArray()));
    }
}